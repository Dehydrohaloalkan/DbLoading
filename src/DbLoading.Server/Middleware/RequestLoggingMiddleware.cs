using System.Security.Claims;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace DbLoading.Server.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var startedAt = System.Diagnostics.Stopwatch.GetTimestamp();

        var user = context.User?.Identity?.IsAuthenticated == true
            ? (context.User.FindFirst(ClaimTypes.Name)?.Value ?? context.User.Identity?.Name ?? "authenticated")
            : "anonymous";

        try
        {
            await _next(context);
            var elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

            _logger.LogInformation(
                "HTTP {Method} {Path} => {StatusCode} in {ElapsedMs:0.0}ms user={User} traceId={TraceId}",
                context.Request.Method,
                context.Request.Path.Value ?? context.Request.GetDisplayUrl(),
                context.Response.StatusCode,
                elapsedMs,
                user,
                context.TraceIdentifier);
        }
        catch
        {
            var elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

            _logger.LogWarning(
                "HTTP {Method} {Path} threw after {ElapsedMs:0.0}ms user={User} traceId={TraceId}",
                context.Request.Method,
                context.Request.Path.Value ?? context.Request.GetDisplayUrl(),
                elapsedMs,
                user,
                context.TraceIdentifier);

            throw;
        }
    }
}

