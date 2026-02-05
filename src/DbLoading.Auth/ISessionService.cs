using DbLoading.Auth.Models;

namespace DbLoading.Auth;

public interface ISessionService
{
    UserSession CreateSession(string login, string dbPassword, Dictionary<string, string>? customClaims = null);
    UserSession? GetSession(string userId);
    void RevokeSession(string userId);
    bool ValidateSessionVersion(string userId, int sessionVersion);
}
