using System.ComponentModel.DataAnnotations;

namespace LeavePortal.Core.DTOs.Auth;

// Request body for login. Simple field checks via DataAnnotations (auto 400).
public class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}
