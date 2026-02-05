using System.Security.Claims;
using DbLoading.Application.Auth;
using DbLoading.Domain.Auth;
using AuthLib = DbLoading.Auth;
using AuthModels = DbLoading.Auth.Models;

namespace DbLoading.Infrastructure.Auth;

public class TokenService : ITokenService
{
    private readonly AuthLib.ITokenService _authTokenService;

    public TokenService(AuthLib.ITokenService authTokenService)
    {
        _authTokenService = authTokenService;
    }

    public string GenerateAccessToken(UserSession session)
    {
        var authSession = ToAuthSession(session);
        return _authTokenService.GenerateAccessToken(authSession);
    }

    public string GenerateRefreshToken(UserSession session)
    {
        var authSession = ToAuthSession(session);
        return _authTokenService.GenerateRefreshToken(authSession);
    }

    public ClaimsPrincipal? ValidateRefreshToken(string token)
    {
        return _authTokenService.ValidateRefreshToken(token);
    }

    public string? GetUserIdFromToken(ClaimsPrincipal principal)
    {
        return _authTokenService.GetUserIdFromToken(principal);
    }

    public int? GetSessionVersionFromToken(ClaimsPrincipal principal)
    {
        return _authTokenService.GetSessionVersionFromToken(principal);
    }

    private static AuthModels.UserSession ToAuthSession(UserSession session)
    {
        return new AuthModels.UserSession
        {
            UserId = session.UserId,
            Login = session.Login,
            DbPassword = session.DbPassword,
            SessionVersion = session.SessionVersion,
            CreatedAt = session.CreatedAt,
            CustomClaims = new Dictionary<string, string>
            {
                ["databaseId"] = session.DatabaseId,
                ["managerId"] = session.ManagerId,
                ["streamId"] = session.StreamId
            }
        };
    }
}
