using DbLoading.Application.Runs;
using DbLoading.Domain.Runs;
using DbLoading.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DbLoading.Server.Runs;

// ЗАГЛУШКА: Реализация IRunEventBroadcaster в Server слое для избежания циклической зависимости.
// Infrastructure не может ссылаться на Server, поэтому реализация находится здесь.
public class RunEventBroadcaster : IRunEventBroadcaster
{
    private readonly IHubContext<RunsHub> _hubContext;
    private readonly ILogger<RunEventBroadcaster> _logger;

    public RunEventBroadcaster(IHubContext<RunsHub> hubContext, ILogger<RunEventBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastRunUpdatedAsync(string runId, RunStatus status, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Broadcast run.updated runId={RunId} status={Status}", runId, status);
        await _hubContext.Clients.Group($"runs:{runId}").SendAsync("run.updated", new
        {
            runId,
            status = status.ToString(),
            updatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task BroadcastGroupUpdatedAsync(string runId, string groupId, GroupStatus status, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Broadcast group.updated runId={RunId} groupId={GroupId} status={Status}", runId, groupId, status);
        await _hubContext.Clients.Group($"runs:{runId}").SendAsync("group.updated", new
        {
            runId,
            groupId,
            status = status.ToString()
        }, cancellationToken);
    }

    public async Task BroadcastScriptUpdatedAsync(string runId, string groupId, string scriptId, ScriptStatus status, string? message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Broadcast script.updated runId={RunId} groupId={GroupId} scriptId={ScriptId} status={Status} message={Message}",
            runId,
            groupId,
            scriptId,
            status,
            message);

        await _hubContext.Clients.Group($"runs:{runId}").SendAsync("script.updated", new
        {
            runId,
            groupId,
            scriptId,
            status = status.ToString(),
            message
        }, cancellationToken);
    }
}