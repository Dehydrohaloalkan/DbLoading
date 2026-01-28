namespace DbLoading.Domain.Runs;

public enum RunStatus
{
    Queued,
    Running,
    Success,
    NoData,
    Failed,
    Cancelled
}