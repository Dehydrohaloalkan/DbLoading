namespace DbLoading.Domain.Runs;

public enum ScriptStatus
{
    Queued,
    Running,
    Success,
    NoData,
    Failed,
    Cancelled
}