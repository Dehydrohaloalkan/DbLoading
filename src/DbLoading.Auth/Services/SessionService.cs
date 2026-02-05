using DbLoading.Auth.Models;

namespace DbLoading.Auth.Services;

public class SessionService : ISessionService
{
    private readonly Dictionary<string, UserSession> _sessions = new();
    private readonly Dictionary<string, int> _sessionVersions = new();
    private readonly object _lock = new();
    private readonly Func<string, Dictionary<string, string>?, string>? _userIdGenerator;

    public SessionService(Func<string, Dictionary<string, string>?, string>? userIdGenerator = null)
    {
        _userIdGenerator = userIdGenerator;
    }

    public UserSession CreateSession(string login, string dbPassword, Dictionary<string, string>? customClaims = null)
    {
        var userId = _userIdGenerator?.Invoke(login, customClaims) ?? login;

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
                DbPassword = dbPassword,
                SessionVersion = _sessionVersions[userId],
                CreatedAt = DateTime.UtcNow,
                CustomClaims = customClaims ?? new Dictionary<string, string>()
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
