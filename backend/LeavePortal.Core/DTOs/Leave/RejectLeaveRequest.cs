namespace LeavePortal.Core.DTOs.Leave;

// Request body for rejecting a leave application.
// The comment is required (enforced by RejectLeaveCommandValidator) so the employee
// always learns why. Application id comes from the route, manager id from JWT claims.
public class RejectLeaveRequest
{
    public string Comment { get; set; } = string.Empty;
}
