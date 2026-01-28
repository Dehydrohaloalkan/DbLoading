using DbLoading.Application.Catalog;
using DbLoading.Domain.Catalog;
using DbLoading.Infrastructure.Config;
using Microsoft.Extensions.Logging;

namespace DbLoading.Infrastructure.Catalog;

public class CatalogService : ICatalogService
{
    private readonly ConfigReader _configReader;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(ConfigReader configReader, ILogger<CatalogService> logger)
    {
        _configReader = configReader;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DatabaseInfo>> GetDatabasesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Catalog: reading databases.json");
        var configs = await _configReader.ReadAsync<List<DatabaseConfig>>("databases.json", cancellationToken);
        _logger.LogInformation("Catalog: databases loaded count={Count}", configs.Count);
        return configs.Select(c => new DatabaseInfo(
            c.Id,
            c.DisplayName,
            c.Server,
            c.Database
        )).ToList();
    }

    public async Task<(IReadOnlyList<ManagerInfo> Managers, IReadOnlyList<StreamInfo> Streams)> GetStreamsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Catalog: reading streams.json");
        var config = await _configReader.ReadAsync<StreamsConfig>("streams.json", cancellationToken);
        var managers = config.Managers.Select(m => new ManagerInfo(m.Id, m.DisplayName)).ToList();
        var streams = config.Streams.Select(s => new StreamInfo(s.Id, s.DisplayName)).ToList();
        _logger.LogInformation("Catalog: streams loaded managers={Managers} streams={Streams}", managers.Count, streams.Count);
        return (managers, streams);
    }

    public async Task<(IReadOnlyList<ScriptGroup> Groups, IReadOnlyList<ColumnProfile> ColumnsProfiles)> GetScriptsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Catalog: reading scripts.json and columns.json");
        var scriptsConfig = await _configReader.ReadAsync<ScriptsConfig>("scripts.json", cancellationToken);
        var columnsConfig = await _configReader.ReadAsync<ColumnsConfig>("columns.json", cancellationToken);

        var groups = scriptsConfig.Groups.Select(g => new ScriptGroup(
            g.Id,
            g.DisplayName,
            g.Scripts.Select(s => new ScriptInfo(
                s.Id,
                s.DisplayName,
                s.ExecutionLane,
                s.ColumnsProfileId
            )).ToList()
        )).ToList();

        var profiles = columnsConfig.Profiles.Select(p => new ColumnProfile(
            p.Id,
            p.Items.Select(i => new ColumnItem(
                i.Id,
                i.Label,
                i.Expression
            )).ToList()
        )).ToList();

        _logger.LogInformation("Catalog: scripts loaded groups={Groups} profiles={Profiles}", groups.Count, profiles.Count);
        return (groups, profiles);
    }
}
