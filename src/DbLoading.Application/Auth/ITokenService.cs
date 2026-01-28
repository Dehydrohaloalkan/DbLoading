using System.Security.Claims;
using DbLoading.Domain.Auth;

namespace DbLoading.Application.Auth;

public interface ITokenService
{
    string GenerateAccessToken(UserSession session);
    string GenerateRefreshToken(UserSession session);
    ClaimsPrincipal? ValidateRefreshToken(string token);
    string? GetUserIdFromToken(ClaimsPrincipal principal);
    int? GetSessionVersionFromToken(ClaimsPrincipal principal);
}
