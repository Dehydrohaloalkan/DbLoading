using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace DbLoading.Server.Middleware;

public sealed class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionLoggingMiddleware> _logger;

    public ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Request aborted by client {Method} {Url} traceId={TraceId}",
                context.Request.Method,
                context.Request.GetDisplayUrl(),
                context.TraceIdentifier);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception for {Method} {Url} traceId={TraceId}",
                context.Request.Method,
                context.Request.GetDisplayUrl(),
                context.TraceIdentifier);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new { error = "Internal server error", traceId = context.TraceIdentifier });
            await context.Response.WriteAsync(payload);
        }
    }
}

