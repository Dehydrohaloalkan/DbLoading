namespace DbLoading.Infrastructure.Export;

public static class OutputCleanup
{
    public const string BeforeRunAlways = "BeforeRunAlways";
    public const string BeforeRunIfPreviousSucceeded = "BeforeRunIfPreviousSucceeded";
    public const string Never = "Never";

    public static void Apply(
        string outputRoot,
        string cleanupPolicy,
        string? lastRunId,
        bool lastRunSucceeded)
    {
        if (cleanupPolicy == Never || string.IsNullOrEmpty(lastRunId))
            return;
        if (cleanupPolicy == BeforeRunIfPreviousSucceeded && !lastRunSucceeded)
            return;

        var path = Path.Combine(outputRoot, lastRunId);
        if (!Directory.Exists(path))
            return;
        Directory.Delete(path, recursive: true);
    }
}
