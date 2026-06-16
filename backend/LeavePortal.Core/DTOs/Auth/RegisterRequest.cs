using System.ComponentModel.DataAnnotations;

namespace LeavePortal.Core.DTOs.Auth;

// Request body for registration.
// Field rules are DataAnnotations — [ApiController] checks them automatically and returns
// 400 with these messages BEFORE the controller action runs. No FluentValidation needed.
public class RegisterRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    [AllowedValues("Employee", "Manager", ErrorMessage = "Role must be Employee or Manager.")]
    public string Role { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "A valid department must be selected.")]
    public int DepartmentId { get; set; }
}
