using LeavePortal.Core.DTOs.Auth;

namespace LeavePortal.Core.Interfaces;

// Auth business logic — registration and login.
// Behind an interface so controllers depend on the abstraction, not the concrete class.
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
