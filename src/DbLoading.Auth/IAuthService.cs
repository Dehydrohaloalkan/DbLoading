using DbLoading.Auth.Models;

namespace DbLoading.Auth;

public interface IAuthService
{
    Task<AuthResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        string server,
        string database,
        CancellationToken cancellationToken = default);
    
    AuthResult<RefreshResponse> Refresh(string? refreshToken);
    void Logout(string userId);
    UserSession? GetSession(string userId);
}
