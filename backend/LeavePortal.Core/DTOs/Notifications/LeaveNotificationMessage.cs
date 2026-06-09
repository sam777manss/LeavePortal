namespace LeavePortal.Core.DTOs.Notifications;

// Contract for the message placed on the Service Bus queue when a leave event happens.
// The Azure Function (Day 5) will read this exact shape and send an email.
// Kept as plain primitives (strings) so it serializes cleanly to JSON across services.
public class LeaveNotificationMessage
{
    public int LeaveApplicationId { get; set; }

    // What happened — e.g. "LeaveApplied", "LeaveCancelled", "LeaveApproved", "LeaveRejected"
    public string EventType { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;

    public string LeaveType { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int TotalDays { get; set; }

    public string Status { get; set; } = string.Empty;
}
