using DbLoading.Domain.Runs;

namespace DbLoading.Application.Runs;

public interface IRunService
{
    Task<Run> StartRunAsync(UserContext userContext, RunSelection selection, CancellationToken cancellationToken);
    Task<Run?> GetRunAsync(string runId, CancellationToken cancellationToken);
    Task<bool> CancelRunAsync(string runId, CancellationToken cancellationToken);
}