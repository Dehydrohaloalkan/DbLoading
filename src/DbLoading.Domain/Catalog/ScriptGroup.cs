namespace DbLoading.Domain.Catalog;

public record ScriptGroup(
    string Id,
    string DisplayName,
    IReadOnlyList<ScriptInfo> Scripts
);
