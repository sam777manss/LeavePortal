namespace LeavePortal.Core.DTOs.Notifications;

// Contract for the message placed on the Service Bus queue when a leave event happens.
// The Azure Function (Day 5) will read this exact shape and send an email.
// Kept as plain primitives (strings) so it serializes cleanly to JSON across services.
public class LeaveNotificationMessage
{
    public int LeaveApplicationId { get; set; }

    // What happened — e.g. "LeaveApplied", "LeaveCancelled", "LeaveApproved", "LeaveRejected"
    public string EventType { get; set; } = string.Empty;

    // WHO this email goes to. Set by the handler:
    //   apply / cancel   -> a department manager (one message per manager)
    //   approve / reject -> the employee
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;

    public string LeaveType { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int TotalDays { get; set; }

    public string Status { get; set; } = string.Empty;
    // Manager's comment on approve/reject — lets the email explain WHY. Null otherwise.
    public string? ReviewComment { get; set; }
}
