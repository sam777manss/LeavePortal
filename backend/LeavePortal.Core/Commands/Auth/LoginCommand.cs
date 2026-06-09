using LeavePortal.Core.DTOs.Auth;
using MediatR;

namespace LeavePortal.Core.Commands.Auth;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<AuthResponse>;
