using LeavePortal.Core.DTOs.Auth;
using MediatR;

namespace LeavePortal.Core.Commands.Auth;

public record RegisterCommand(
    string FullName,
    string Email,
    string Password,
    string Role,
    int DepartmentId
) : IRequest<AuthResponse>;
