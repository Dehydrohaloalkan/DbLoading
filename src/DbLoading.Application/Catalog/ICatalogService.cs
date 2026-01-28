using DbLoading.Domain.Catalog;

namespace DbLoading.Application.Catalog;

public interface ICatalogService
{
    Task<IReadOnlyList<DatabaseInfo>> GetDatabasesAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ManagerInfo> Managers, IReadOnlyList<StreamInfo> Streams)> GetStreamsAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ScriptGroup> Groups, IReadOnlyList<ColumnProfile> ColumnsProfiles)> GetScriptsAsync(CancellationToken cancellationToken = default);
}
