namespace DbLoading.Domain.Catalog;

public record DatabaseInfo(
    string Id,
    string DisplayName,
    string Server,
    string Database
);
