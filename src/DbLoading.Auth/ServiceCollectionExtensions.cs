using System.Text;
using DbLoading.Auth.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DbLoading.Auth;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDbLoadingAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        Func<string, Dictionary<string, string>?, string>? userIdGenerator = null,
        Action<JwtBearerEvents>? configureJwtEvents = null)
    {
        var jwtSecretKey = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey is not configured");
        var key = Encoding.UTF8.GetBytes(jwtSecretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            if (configureJwtEvents != null)
            {
                options.Events = new JwtBearerEvents();
                configureJwtEvents(options.Events);
            }
        });

        services.AddAuthorization();

        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<ISessionService>(_ => new SessionService(userIdGenerator));
        services.AddSingleton<IAuthService, AuthService>();

        return services;
    }
}
