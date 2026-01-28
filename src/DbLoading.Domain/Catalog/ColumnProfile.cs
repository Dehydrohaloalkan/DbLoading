namespace DbLoading.Domain.Catalog;

public record ColumnProfile(
    string Id,
    IReadOnlyList<ColumnItem> Items
);
