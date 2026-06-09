using LeavePortal.Core.Commands.Auth;
using LeavePortal.Core.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LeavePortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IJwtService _jwtService;

    public AuthController(IMediator mediator, IJwtService jwtService)
    {
        _mediator = mediator;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            // Generate JWT and set as HttpOnly Cookie
            var token = _jwtService.GenerateToken(result);

            Response.Cookies.Append("jwt", token, new CookieOptions
            {
                HttpOnly = true,   // JavaScript cannot read this cookie
                Secure = true,     // Only sent over HTTPS
                SameSite = SameSiteMode.Strict, // CSRF protection
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("jwt");
        return Ok(new { message = "Logged out successfully." });
    }

    // Protected endpoint — only works if a valid JWT cookie is present.
    // [Authorize] reads the jwt cookie, validates it, and rejects with 401 if missing/invalid.
    // Reads the logged-in user's info from the token claims — proves auth works end to end.
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);
        var name = User.FindFirstValue(ClaimTypes.Name);

        return Ok(new { id, email, role, name });
    }

    // Manager-only endpoint — proves role-based authorization works.
    // An Employee's token will be rejected with 403 Forbidden here.
    [Authorize(Roles = "Manager")]
    [HttpGet("manager-only")]
    public IActionResult ManagerOnly()
    {
        return Ok(new { message = "You are a Manager — you can see this." });
    }
}
