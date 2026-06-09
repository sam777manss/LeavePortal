namespace LeavePortal.Core.DTOs.Leave;

// What the API returns to the client for a leave application.
// We never return the raw EF entity (which carries navigation properties and DB concerns) —
// we project into this clean DTO. Includes the human-readable LeaveTypeName so the
// frontend does not need a second call to resolve the leave type.
public class LeaveApplicationDto
{
    public int Id { get; set; }
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? DocumentUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReviewComment { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
