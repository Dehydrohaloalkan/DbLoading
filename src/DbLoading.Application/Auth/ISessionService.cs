using DbLoading.Domain.Auth;

namespace DbLoading.Application.Auth;

public interface ISessionService
{
    UserSession CreateSession(string login, string dbPassword, string databaseId, string managerId, string streamId);
    UserSession? GetSession(string userId);
    void RevokeSession(string userId);
    bool ValidateSessionVersion(string userId, int sessionVersion);
}
