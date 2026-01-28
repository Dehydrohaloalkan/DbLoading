namespace DbLoading.Domain.Runs;

public enum GroupStatus
{
    Queued,
    Running,
    Success,
    NoData,
    Failed,
    Cancelled
}