using DbLoading.Application.Auth;
using DbLoading.Domain.Auth;

namespace DbLoading.Infrastructure.Auth;

public class SessionService : ISessionService
{
    private readonly Dictionary<string, UserSession> _sessions = new();
    private readonly Dictionary<string, int> _sessionVersions = new();
    private readonly object _lock = new();

    public UserSession CreateSession(string login, string dbPassword, string databaseId, string managerId, string streamId)
    {
        var userId = $"{login}@{databaseId}#{managerId}@{streamId}";

        lock (_lock)
        {
            if (!_sessionVersions.ContainsKey(userId))
            {
                _sessionVersions[userId] = 0;
            }

            var session = new UserSession
            {
                UserId = userId,
                Login = login,
                DatabaseId = databaseId,
                ManagerId = managerId,
                StreamId = streamId,
                DbPassword = dbPassword,
                SessionVersion = _sessionVersions[userId],
                CreatedAt = DateTime.UtcNow
            };

            _sessions[userId] = session;
            return session;
        }
    }

    public UserSession? GetSession(string userId)
    {
        lock (_lock)
        {
            return _sessions.TryGetValue(userId, out var session) ? session : null;
        }
    }

    public void RevokeSession(string userId)
    {
        lock (_lock)
        {
            if (_sessionVersions.ContainsKey(userId))
            {
                _sessionVersions[userId]++;
            }
            _sessions.Remove(userId);
        }
    }

    public bool ValidateSessionVersion(string userId, int sessionVersion)
    {
        lock (_lock)
        {
            if (!_sessionVersions.TryGetValue(userId, out var currentVersion))
            {
                return false;
            }
            return currentVersion == sessionVersion;
        }
    }
}
