using System.ComponentModel.DataAnnotations;

namespace LeavePortal.Core.DTOs.Leave;

// Request body for rejecting a leave application.
// The comment is required so the employee always learns why. [Required] catches null/empty;
// LeaveService also rejects whitespace-only. Application id comes from the route, manager id
// from the JWT claims.
public class RejectLeaveRequest
{
    [Required(ErrorMessage = "A comment is required when rejecting a leave application.")]
    [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
    public string Comment { get; set; } = string.Empty;
}
