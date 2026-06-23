using System.Text.Json;
using LeavePortal.Core.DTOs.Notifications;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace LeavePortal.Functions;

// The consumer side of the async pipeline.
// Triggered automatically whenever a message lands in the 'leave-notifications' queue:
//   1. deserialize the message, 2. build an email, 3. send it via Gmail SMTP,
//   4. write a NotificationLogs row recording Sent/Failed.
public class LeaveNotificationFunction
{
    private readonly IConfiguration _config;
    private readonly ILogger<LeaveNotificationFunction> _logger;

    public LeaveNotificationFunction(IConfiguration config, ILogger<LeaveNotificationFunction> logger)
    {
        _config = config;
        _logger = logger;
    }

    [Function("LeaveNotificationFunction")]
    public async Task Run([ServiceBusTrigger("leave-notifications", Connection = "ServiceBusConnection")] string messageBody)
    {
        _logger.LogInformation("Received message: {Body}", messageBody);

        // 1. Deserialize the JSON into the exact shape the API published.
        var msg = JsonSerializer.Deserialize<LeaveNotificationMessage>(
            messageBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (msg is null)
        {
            _logger.LogError("Message could not be deserialized — skipping.");
            return;
        }

        // 2. Build subject + body from the event type.
        var (subject, body) = BuildEmail(msg);

        // 3. Try to send; record the outcome either way.
        string status;
        string? errorMessage = null;
        try
        {
            await SendEmailAsync(msg.RecipientEmail, msg.RecipientName, subject, body);
            status = "Sent";
            _logger.LogInformation("Email sent to {Email}", msg.RecipientEmail);
        }
        catch (Exception ex)
        {
            status = "Failed";
            errorMessage = ex.Message;
            _logger.LogError(ex, "Failed to send email to {Email}", msg.RecipientEmail);
        }

        // 4. Audit row — so we can SEE whether notifications actually went out.
        await LogNotificationAsync(msg.LeaveApplicationId, msg.RecipientEmail, subject, status, errorMessage);
    }

    // Picks the subject + body based on what happened.
    private static (string Subject, string Body) BuildEmail(LeaveNotificationMessage m)
    {
        var dates = $"{m.StartDate} to {m.EndDate} ({m.TotalDays} day(s))";

        return m.EventType switch
        {
            "LeaveApplied" => (
                "New leave request pending your review",
                $"Hi {m.RecipientName},\n\n{m.EmployeeName} has applied for {m.LeaveType} ({dates}).\n\nPlease review it in LeavePortal.\n\n— LeavePortal"),

            "LeaveCancelled" => (
                "A leave request was cancelled",
                $"Hi {m.RecipientName},\n\n{m.EmployeeName} has cancelled their {m.LeaveType} request ({dates}).\n\n— LeavePortal"),

            "LeaveApproved" => (
                "Your leave has been approved",
                $"Hi {m.RecipientName},\n\nYour {m.LeaveType} request ({dates}) has been APPROVED."
                + (string.IsNullOrWhiteSpace(m.ReviewComment) ? "" : $"\n\nManager's comment: {m.ReviewComment}")
                + "\n\n— LeavePortal"),

            "LeaveRejected" => (
                "Your leave has been rejected",
                $"Hi {m.RecipientName},\n\nYour {m.LeaveType} request ({dates}) has been REJECTED."
                + (string.IsNullOrWhiteSpace(m.ReviewComment) ? "" : $"\n\nReason: {m.ReviewComment}")
                + "\n\n— LeavePortal"),

            _ => (
                "LeavePortal notification",
                $"Hi {m.RecipientName},\n\nUpdate on a {m.LeaveType} request ({dates}). Status: {m.Status}.\n\n— LeavePortal")
        };
    }

    // Sends one email via Gmail SMTP using MailKit.
    private async Task SendEmailAsync(string toEmail, string toName, string subject, string body)
    {
        var host = _config["Email:SmtpHost"]!;
        var port = int.Parse(_config["Email:SmtpPort"]!);
        var senderEmail = _config["Email:SenderEmail"]!;
        var senderName = _config["Email:SenderName"] ?? "LeavePortal";
        var appPassword = _config["Email:AppPassword"]!;

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(senderName, senderEmail));
        email.To.Add(new MailboxAddress(toName, toEmail));
        email.Subject = subject;
        email.Body = new TextPart("plain") { Text = body };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(senderEmail, appPassword);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }

    // Writes the Sent/Failed audit row using lightweight ADO.NET (no EF, Core-only).
    private async Task LogNotificationAsync(int leaveApplicationId, string recipientEmail, string subject, string status, string? errorMessage)
    {
        var connectionString = _config["SqlConnectionString"]!;

        const string sql = @"
            INSERT INTO NotificationLogs (LeaveApplicationId, RecipientEmail, Subject, Status, ErrorMessage)
            VALUES (@LeaveApplicationId, @RecipientEmail, @Subject, @Status, @ErrorMessage);";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@LeaveApplicationId", leaveApplicationId);
        command.Parameters.AddWithValue("@RecipientEmail", recipientEmail);
        command.Parameters.AddWithValue("@Subject", subject);
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@ErrorMessage", (object?)errorMessage ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }
}
