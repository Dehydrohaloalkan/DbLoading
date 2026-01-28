using DbLoading.Domain.Runs;

namespace DbLoading.Application.Runs;

public interface IRunEventBroadcaster
{
    Task BroadcastRunUpdatedAsync(string runId, RunStatus status, CancellationToken cancellationToken);
    Task BroadcastGroupUpdatedAsync(string runId, string groupId, GroupStatus status, CancellationToken cancellationToken);
    Task BroadcastScriptUpdatedAsync(string runId, string groupId, string scriptId, ScriptStatus status, string? message, CancellationToken cancellationToken);
}