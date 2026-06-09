using LeavePortal.Core.DTOs.Auth;

namespace LeavePortal.Core.Interfaces;

public interface IJwtService
{
    string GenerateToken(AuthResponse user);
}
