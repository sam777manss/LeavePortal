namespace LeavePortal.Core.DTOs.Leave;

// What a manager sees when reviewing pending applications.
// Differs from LeaveApplicationDto by including WHO applied (EmployeeName / EmployeeEmail) —
// a manager reviewing a list needs to know the employee, whereas an employee viewing
// their own list already knows it is theirs.
public class PendingLeaveDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
