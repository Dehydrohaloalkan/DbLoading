using System.Security.Claims;
using DbLoading.Application.Auth;
using DbLoading.Database;
using DbLoading.Domain.Auth;
using DbLoading.Infrastructure.Config;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DbLoading.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly ISessionService _sessionService;
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ConfigReader _configReader;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ITokenService tokenService,
        ISessionService sessionService,
        IDbConnectionFactory dbConnectionFactory,
        ConfigReader configReader,
        ILogger<AuthController> logger)
    {
        _tokenService = tokenService;
        _sessionService = sessionService;
        _dbConnectionFactory = dbConnectionFactory;
        _configReader = configReader;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DbUsername) ||
            string.IsNullOrWhiteSpace(request.DbPassword) ||
            string.IsNullOrWhiteSpace(request.DatabaseId) ||
            string.IsNullOrWhiteSpace(request.ManagerId) ||
            string.IsNullOrWhiteSpace(request.StreamId))
        {
            return BadRequest(new { error = "All fields are required" });
        }

        try
        {
            var databases = _configReader.LoadDatabases();
            var dbConfig = databases.FirstOrDefault(d => d.Id == request.DatabaseId);
            if (dbConfig == null)
            {
                return BadRequest(new { error = "Invalid database ID" });
            }

            await using var connection = await _dbConnectionFactory.CreateAsync(
                dbConfig.Server,
                dbConfig.Database,
                request.DbUsername,
                request.DbPassword,
                cancellationToken);

            await connection.ConnectAsync(cancellationToken);

            if (!connection.IsConnected)
            {
                return Unauthorized(new { error = "Failed to connect to database" });
            }

            var session = _sessionService.CreateSession(
                request.DbUsername,
                request.DbPassword,
                request.DatabaseId,
                request.ManagerId,
                request.StreamId
            );

            var accessToken = _tokenService.GenerateAccessToken(session);
            var refreshToken = _tokenService.GenerateRefreshToken(session);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddHours(4)
            };

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

            _logger.LogInformation("User {Login} logged in for database {DatabaseId}", 
                request.DbUsername, request.DatabaseId);

            return Ok(new LoginResponse(
                accessToken,
                new UserDto(
                    session.Login,
                    session.DatabaseId,
                    session.ManagerId,
                    session.StreamId
                )
            ));
        }
        catch (DbConnectionException ex)
        {
            _logger.LogWarning(ex, "Database connection failed for user {Login}", request.DbUsername);
            return Unauthorized(new { error = "Invalid credentials or database unavailable" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Login}", request.DbUsername);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("refresh")]
    public IActionResult Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new { error = "Refresh token not found" });
        }

        var principal = _tokenService.ValidateRefreshToken(refreshToken);
        if (principal == null)
        {
            Response.Cookies.Delete("refreshToken");
            return Unauthorized(new { error = "Invalid refresh token" });
        }

        var userId = _tokenService.GetUserIdFromToken(principal);
        var sessionVersion = _tokenService.GetSessionVersionFromToken(principal);

        if (userId == null || sessionVersion == null)
        {
            Response.Cookies.Delete("refreshToken");
            return Unauthorized(new { error = "Invalid token claims" });
        }

        if (!_sessionService.ValidateSessionVersion(userId, sessionVersion.Value))
        {
            Response.Cookies.Delete("refreshToken");
            return Unauthorized(new { error = "Session revoked" });
        }

        var session = _sessionService.GetSession(userId);
        if (session == null)
        {
            Response.Cookies.Delete("refreshToken");
            return Unauthorized(new { error = "Session not found" });
        }

        var newAccessToken = _tokenService.GenerateAccessToken(session);
        var newRefreshToken = _tokenService.GenerateRefreshToken(session);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddHours(4)
        };

        Response.Cookies.Append("refreshToken", newRefreshToken, cookieOptions);

        return Ok(new RefreshResponse(newAccessToken));
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            _sessionService.RevokeSession(userId);
            _logger.LogInformation("User {UserId} logged out", userId);
        }

        Response.Cookies.Delete("refreshToken");
        return Ok(new { message = "Logged out successfully" });
    }
}
