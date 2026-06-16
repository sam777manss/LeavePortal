using System.ComponentModel.DataAnnotations;

namespace LeavePortal.Core.DTOs.Leave;

// This is the shape the CLIENT sends in the request body.
// Note: it does NOT contain UserId. The user is taken from the JWT cookie (claims)
// on the server side — never trusted from the request body. This prevents a user
// from submitting leave on behalf of someone else by spoofing a UserId.
//
// Simple field rules live here as DataAnnotations (auto 400). The cross-field /
// "not in the past" date rules are business rules and live in LeaveService.
public class ApplyLeaveRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid leave type must be selected.")]
    public int LeaveTypeId { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    [Required(ErrorMessage = "Reason is required.")]
    [MaxLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters.")]
    public string Reason { get; set; } = string.Empty;
}
