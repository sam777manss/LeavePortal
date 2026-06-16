using LeavePortal.Core.DTOs.Auth;
using LeavePortal.Core.Interfaces;
using LeavePortal.Infrastructure.Data;
using LeavePortal.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeavePortal.Infrastructure.Services;

// Auth business logic, moved out of the old MediatR handlers into a plain service.
// Controllers call these methods directly — easy to step through in the debugger.
public class AuthService : IAuthService
{
    private readonly LeavePortalDbContext _context;

    public AuthService(LeavePortalDbContext context)
    {
        _context = context;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Email must be unique. The DB also enforces this, but checking here gives a clean message.
        var exists = await _context.Users
            .AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (exists)
            throw new Exception("Email already registered.");

        // Hash the password — never store plain text.
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = passwordHash,
            Role = request.Role,
            DepartmentId = request.DepartmentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Find an active user by email.
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, cancellationToken);

        if (user is null)
            throw new Exception("Invalid email or password.");

        // Verify the password against the stored BCrypt hash.
        var isValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isValid)
            throw new Exception("Invalid email or password.");

        return new AuthResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };
    }
}
