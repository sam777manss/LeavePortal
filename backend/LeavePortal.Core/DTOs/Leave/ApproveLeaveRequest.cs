namespace LeavePortal.Core.DTOs.Leave;

// Request body for approving a leave application.
// Only carries the optional comment — the application id comes from the route and the
// manager id comes from the JWT claims (never trusted from the body).
public class ApproveLeaveRequest
{
    public string? Comment { get; set; }
}
