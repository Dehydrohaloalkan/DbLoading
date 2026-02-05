using DbLoading.Auth.Models;
using DbLoading.Database;

namespace DbLoading.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ITokenService _tokenService;
    private readonly ISessionService _sessionService;

    public AuthService(
        IDbConnectionFactory dbConnectionFactory,
        ITokenService tokenService,
        ISessionService sessionService)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _tokenService = tokenService;
        _sessionService = sessionService;
    }

    public async Task<AuthResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        string server,
        string database,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new AuthResult<LoginResponse>(false, null, "Username and password are required");
        }

        try
        {
            await using var connection = await _dbConnectionFactory.CreateAsync(
                server,
                database,
                request.Username,
                request.Password,
                cancellationToken);

            await connection.ConnectAsync(cancellationToken);

            if (!connection.IsConnected)
            {
                return new AuthResult<LoginResponse>(false, null, "Failed to connect to database");
            }

            var session = _sessionService.CreateSession(
                request.Username,
                request.Password,
                request.CustomClaims);

            var accessToken = _tokenService.GenerateAccessToken(session);

            var response = new LoginResponse(
                accessToken,
                new UserInfo(
                    session.UserId,
                    session.Login,
                    session.CustomClaims
                )
            );

            return new AuthResult<LoginResponse>(true, response);
        }
        catch (DbConnectionException ex)
        {
            return new AuthResult<LoginResponse>(false, null, ex.Message);
        }
        catch (Exception)
        {
            return new AuthResult<LoginResponse>(false, null, "Authentication failed");
        }
    }

    public AuthResult<RefreshResponse> Refresh(string? refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return new AuthResult<RefreshResponse>(false, null, "Refresh token not found");
        }

        var principal = _tokenService.ValidateRefreshToken(refreshToken);
        if (principal == null)
        {
            return new AuthResult<RefreshResponse>(false, null, "Invalid refresh token");
        }

        var userId = _tokenService.GetUserIdFromToken(principal);
        var sessionVersion = _tokenService.GetSessionVersionFromToken(principal);

        if (userId == null || sessionVersion == null)
        {
            return new AuthResult<RefreshResponse>(false, null, "Invalid token claims");
        }

        if (!_sessionService.ValidateSessionVersion(userId, sessionVersion.Value))
        {
            return new AuthResult<RefreshResponse>(false, null, "Session revoked");
        }

        var session = _sessionService.GetSession(userId);
        if (session == null)
        {
            return new AuthResult<RefreshResponse>(false, null, "Session not found");
        }

        var newAccessToken = _tokenService.GenerateAccessToken(session);
        return new AuthResult<RefreshResponse>(true, new RefreshResponse(newAccessToken));
    }

    public void Logout(string userId)
    {
        _sessionService.RevokeSession(userId);
    }

    public UserSession? GetSession(string userId)
    {
        return _sessionService.GetSession(userId);
    }
}
