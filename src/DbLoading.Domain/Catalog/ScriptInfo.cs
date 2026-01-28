namespace DbLoading.Domain.Catalog;

public record ScriptInfo(
    string Id,
    string DisplayName,
    int ExecutionLane,
    string? ColumnsProfileId
);
