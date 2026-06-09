namespace LeavePortal.Core.DTOs.Leave;

// This is the shape the CLIENT sends in the request body.
// Note: it does NOT contain UserId. The user is taken from the JWT cookie (claims)
// on the server side — never trusted from the request body. This prevents a user
// from submitting leave on behalf of someone else by spoofing a UserId.
public class ApplyLeaveRequest
{
    public int LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}
