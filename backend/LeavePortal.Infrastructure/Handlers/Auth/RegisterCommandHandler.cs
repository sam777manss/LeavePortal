using LeavePortal.Core.Commands.Auth;
using LeavePortal.Core.DTOs.Auth;
using LeavePortal.Infrastructure.Data;
using LeavePortal.Infrastructure.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LeavePortal.Infrastructure.Handlers.Auth;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly LeavePortalDbContext _context;

    public RegisterCommandHandler(LeavePortalDbContext context)
    {
        _context = context;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        var exists = await _context.Users
            .AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (exists)
            throw new Exception("Email already registered.");

        // Hash the password — never store plain text
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
}
