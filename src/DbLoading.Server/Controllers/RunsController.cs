using System.Security.Claims;
using DbLoading.Application.Runs;
using DbLoading.Domain.Runs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DbLoading.Server.Controllers;

[ApiController]
[Route("api/runs")]
[Authorize]
public class RunsController : ControllerBase
{
    private readonly IRunService _runService;
    private readonly ILogger<RunsController> _logger;

    public RunsController(IRunService runService, ILogger<RunsController> logger)
    {
        _runService = runService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<StartRunResponseDto>> StartRun(
        [FromBody] StartRunRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userContext = GetUserContext();
            if (userContext == null)
            {
                return Unauthorized(new { error = "User context not found" });
            }

            var selection = MapToRunSelection(request);
            var run = await _runService.StartRunAsync(userContext, selection, cancellationToken);

            _logger.LogInformation("Run {RunId} started by user {Login}", run.RunId, userContext.Login);

            return Ok(new StartRunResponseDto
            {
                RunId = run.RunId,
                Status = run.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting run");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{runId}")]
    public async Task<ActionResult<RunStatusDto>> GetRun(
        string runId,
        CancellationToken cancellationToken)
    {
        try
        {
            var run = await _runService.GetRunAsync(runId, cancellationToken);
            if (run == null)
            {
                return NotFound(new { error = "Run not found" });
            }

            var userContext = GetUserContext();
            if (userContext == null || run.UserContext.Login != userContext.Login)
            {
                return Forbid();
            }

            return Ok(MapToRunStatusDto(run));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting run {RunId}", runId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{runId}/cancel")]
    public async Task<ActionResult> CancelRun(
        string runId,
        CancellationToken cancellationToken)
    {
        try
        {
            var run = await _runService.GetRunAsync(runId, cancellationToken);
            if (run == null)
            {
                return NotFound(new { error = "Run not found" });
            }

            var userContext = GetUserContext();
            if (userContext == null || run.UserContext.Login != userContext.Login)
            {
                return Forbid();
            }

            var cancelled = await _runService.CancelRunAsync(runId, cancellationToken);
            if (!cancelled)
            {
                return BadRequest(new { error = "Run cannot be cancelled" });
            }

            _logger.LogInformation("Run {RunId} cancelled by user {Login}", runId, userContext.Login);

            return Ok(new { message = "Run cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling run {RunId}", runId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private UserContext? GetUserContext()
    {
        var login = User.FindFirst(ClaimTypes.Name)?.Value;
        var databaseId = User.FindFirst("databaseId")?.Value;
        var managerId = User.FindFirst("managerId")?.Value;
        var streamId = User.FindFirst("streamId")?.Value;

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(databaseId) ||
            string.IsNullOrEmpty(managerId) || string.IsNullOrEmpty(streamId))
        {
            return null;
        }

        return new UserContext
        {
            Login = login,
            DatabaseId = databaseId,
            ManagerId = managerId,
            StreamId = streamId
        };
    }

    private RunSelection MapToRunSelection(StartRunRequestDto request)
    {
        return new RunSelection
        {
            Mode = request.Mode == "AllGroups" ? RunMode.AllGroups : RunMode.AllGroups,
            Groups = request.Selection.Groups.Select(g => new GroupSelection
            {
                GroupId = g.GroupId,
                Enabled = g.Enabled,
                Scripts = g.Scripts.Select(s => new ScriptSelection
                {
                    ScriptId = s.ScriptId,
                    Enabled = s.Enabled,
                    ExportMode = s.ExportMode == "CustomColumns" ? ExportMode.CustomColumns : ExportMode.DefaultColumns,
                    SelectedColumnItemIds = s.SelectedColumnItemIds ?? new List<string>()
                }).ToList()
            }).ToList()
        };
    }

    private RunStatusDto MapToRunStatusDto(Run run)
    {
        return new RunStatusDto
        {
            RunId = run.RunId,
            Status = run.Status.ToString(),
            CreatedAt = run.CreatedAt,
            UpdatedAt = run.UpdatedAt,
            GroupStatuses = run.GroupStatuses.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToString()
            ),
            ScriptStatuses = run.ScriptStatuses.ToDictionary(
                groupKvp => groupKvp.Key,
                groupKvp => groupKvp.Value.ToDictionary(
                    scriptKvp => scriptKvp.Key,
                    scriptKvp => scriptKvp.Value.ToString()
                )
            )
        };
    }
}

public class StartRunRequestDto
{
    public required string Mode { get; init; }
    public required RunSelectionDto Selection { get; init; }
}

public class RunSelectionDto
{
    public required List<GroupSelectionDto> Groups { get; init; }
}

public class GroupSelectionDto
{
    public required string GroupId { get; init; }
    public required bool Enabled { get; init; }
    public required List<ScriptSelectionDto> Scripts { get; init; }
}

public class ScriptSelectionDto
{
    public required string ScriptId { get; init; }
    public required bool Enabled { get; init; }
    public required string ExportMode { get; init; }
    public List<string>? SelectedColumnItemIds { get; init; }
}

public class StartRunResponseDto
{
    public required string RunId { get; init; }
    public required string Status { get; init; }
}

public class RunStatusDto
{
    public required string RunId { get; init; }
    public required string Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public required Dictionary<string, string> GroupStatuses { get; init; }
    public required Dictionary<string, Dictionary<string, string>> ScriptStatuses { get; init; }
}