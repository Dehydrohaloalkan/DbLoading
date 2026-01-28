using DbLoading.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DbLoading.Server.Controllers;

[ApiController]
[Route("api/catalog")]
[AllowAnonymous]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(ICatalogService catalogService, ILogger<CatalogController> logger)
    {
        _catalogService = catalogService;
        _logger = logger;
    }

    [HttpGet("databases")]
    public async Task<ActionResult<IReadOnlyList<DatabaseDto>>> GetDatabases(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting databases list");
            var databases = await _catalogService.GetDatabasesAsync(cancellationToken);
            _logger.LogInformation("Retrieved {Count} databases", databases.Count);
            return Ok(databases.Select(d => new DatabaseDto(d.Id, d.DisplayName, d.Server, d.Database)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting databases");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpGet("streams")]
    public async Task<ActionResult<StreamsResponseDto>> GetStreams(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting streams and managers list");
            var (managers, streams) = await _catalogService.GetStreamsAsync(cancellationToken);
            _logger.LogInformation("Retrieved {ManagerCount} managers and {StreamCount} streams", managers.Count, streams.Count);
            return Ok(new StreamsResponseDto
            {
                Managers = managers.Select(m => new ManagerDto(m.Id, m.DisplayName)).ToList(),
                Streams = streams.Select(s => new StreamDto(s.Id, s.DisplayName)).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting streams");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpGet("scripts")]
    public async Task<ActionResult<ScriptsResponseDto>> GetScripts(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting scripts list");
            var (groups, profiles) = await _catalogService.GetScriptsAsync(cancellationToken);
            _logger.LogInformation("Retrieved {GroupCount} groups and {ProfileCount} profiles", groups.Count, profiles.Count);
            return Ok(new ScriptsResponseDto
            {
                Groups = groups.Select(g => new ScriptGroupDto
                {
                    Id = g.Id,
                    DisplayName = g.DisplayName,
                    Scripts = g.Scripts.Select(s => new ScriptDto
                    {
                        Id = s.Id,
                        DisplayName = s.DisplayName,
                        ExecutionLane = s.ExecutionLane,
                        ColumnsProfileId = s.ColumnsProfileId
                    }).ToList()
                }).ToList(),
                ColumnsProfiles = profiles.Select(p => new ColumnProfileDto
                {
                    Id = p.Id,
                    Items = p.Items.Select(i => new ColumnItemDto
                    {
                        Id = i.Id,
                        Label = i.Label
                    }).ToList()
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scripts");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }
}

// DTOs
public record DatabaseDto(string Id, string DisplayName, string Server, string Database);

public class StreamsResponseDto
{
    public required List<ManagerDto> Managers { get; init; }
    public required List<StreamDto> Streams { get; init; }
}

public record ManagerDto(string Id, string DisplayName);

public record StreamDto(string Id, string DisplayName);

public class ScriptsResponseDto
{
    public required List<ScriptGroupDto> Groups { get; init; }
    public required List<ColumnProfileDto> ColumnsProfiles { get; init; }
}

public class ScriptGroupDto
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required List<ScriptDto> Scripts { get; init; }
}

public class ScriptDto
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required int ExecutionLane { get; init; }
    public string? ColumnsProfileId { get; init; }
}

public class ColumnProfileDto
{
    public required string Id { get; init; }
    public required List<ColumnItemDto> Items { get; init; }
}

public class ColumnItemDto
{
    public required string Id { get; init; }
    public required string Label { get; init; }
}
