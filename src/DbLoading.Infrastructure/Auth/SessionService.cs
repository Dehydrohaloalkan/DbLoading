using DbLoading.Application.Auth;
using DbLoading.Domain.Auth;
using AuthLib = DbLoading.Auth;

namespace DbLoading.Infrastructure.Auth;

public class SessionService : ISessionService
{
    private readonly AuthLib.ISessionService _authSessionService;

    public SessionService(AuthLib.ISessionService authSessionService)
    {
        _authSessionService = authSessionService;
    }

    public UserSession CreateSession(string login, string dbPassword, string databaseId, string managerId, string streamId)
    {
        var customClaims = new Dictionary<string, string>
        {
            ["databaseId"] = databaseId,
            ["managerId"] = managerId,
            ["streamId"] = streamId
        };

        var authSession = _authSessionService.CreateSession(login, dbPassword, customClaims);
        
        return new UserSession
        {
            UserId = authSession.UserId,
            Login = authSession.Login,
            DatabaseId = databaseId,
            ManagerId = managerId,
            StreamId = streamId,
            DbPassword = authSession.DbPassword,
            SessionVersion = authSession.SessionVersion,
            CreatedAt = authSession.CreatedAt
        };
    }

    public UserSession? GetSession(string userId)
    {
        var authSession = _authSessionService.GetSession(userId);
        if (authSession == null) return null;

        return new UserSession
        {
            UserId = authSession.UserId,
            Login = authSession.Login,
            DatabaseId = authSession.CustomClaims.GetValueOrDefault("databaseId") ?? string.Empty,
            ManagerId = authSession.CustomClaims.GetValueOrDefault("managerId") ?? string.Empty,
            StreamId = authSession.CustomClaims.GetValueOrDefault("streamId") ?? string.Empty,
            DbPassword = authSession.DbPassword,
            SessionVersion = authSession.SessionVersion,
            CreatedAt = authSession.CreatedAt
        };
    }

    public void RevokeSession(string userId)
    {
        _authSessionService.RevokeSession(userId);
    }

    public bool ValidateSessionVersion(string userId, int sessionVersion)
    {
        return _authSessionService.ValidateSessionVersion(userId, sessionVersion);
    }
}
