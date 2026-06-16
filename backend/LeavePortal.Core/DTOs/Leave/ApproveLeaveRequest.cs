using System.ComponentModel.DataAnnotations;

namespace LeavePortal.Core.DTOs.Leave;

// Request body for approving a leave application.
// Only carries the optional comment — the application id comes from the route and the
// manager id comes from the JWT claims (never trusted from the body).
public class ApproveLeaveRequest
{
    [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
    public string? Comment { get; set; }
}
