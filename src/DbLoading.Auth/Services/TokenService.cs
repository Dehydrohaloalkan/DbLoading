using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DbLoading.Auth.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DbLoading.Auth.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly SymmetricSecurityKey _key;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _tokenHandler = new JwtSecurityTokenHandler();
        var secretKey = _configuration["Jwt:SecretKey"] 
            ?? throw new InvalidOperationException("Jwt:SecretKey is not configured");
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    }

    public string GenerateAccessToken(UserSession session)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId),
            new(ClaimTypes.Name, session.Login),
            new("sv", session.SessionVersion.ToString())
        };

        foreach (var (key, value) in session.CustomClaims)
        {
            claims.Add(new Claim(key, value));
        }

        var lifetimeMinutes = _configuration.GetValue<int>("Jwt:AccessTokenLifetimeMinutes", 20);
        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(lifetimeMinutes),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken(UserSession session)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId),
            new("sv", session.SessionVersion.ToString())
        };

        var lifetimeHours = _configuration.GetValue<int>("Jwt:RefreshTokenLifetimeHours", 4);
        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(lifetimeHours),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateRefreshToken(string token)
    {
        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            return _tokenHandler.ValidateToken(token, validationParameters, out _);
        }
        catch
        {
            return null;
        }
    }

    public string? GetUserIdFromToken(ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public int? GetSessionVersionFromToken(ClaimsPrincipal principal)
    {
        var svClaim = principal.FindFirst("sv")?.Value;
        return int.TryParse(svClaim, out var sv) ? sv : null;
    }
}
