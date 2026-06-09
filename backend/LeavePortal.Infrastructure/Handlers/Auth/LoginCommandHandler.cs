using LeavePortal.Core.Commands.Auth;
using LeavePortal.Core.DTOs.Auth;
using LeavePortal.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LeavePortal.Infrastructure.Handlers.Auth;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly LeavePortalDbContext _context;

    public LoginCommandHandler(LeavePortalDbContext context)
    {
        _context = context;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Find active user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive == true, cancellationToken);

        if (user == null)
            throw new Exception("Invalid email or password.");

        // Verify password against stored BCrypt hash
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
