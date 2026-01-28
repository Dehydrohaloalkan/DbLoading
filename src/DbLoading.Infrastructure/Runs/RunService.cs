using System.Collections.Concurrent;
using System.Text;
using DbLoading.Application.Db2;
using DbLoading.Application.Export;
using DbLoading.Application.Runs;
using DbLoading.Domain.Runs;
using DbLoading.Infrastructure.Config;
using DbLoading.Infrastructure.Export;
using Microsoft.Extensions.Logging;

namespace DbLoading.Infrastructure.Runs;

public class RunService : IRunService
{
    private readonly ConcurrentDictionary<string, Run> _runs = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _runCts = new();
    private readonly IRunEventBroadcaster _broadcaster;
    private readonly ILogger<RunService> _logger;
    private readonly IOutputWriter _outputWriter;
    private readonly ISqlModifier _sqlModifier;
    private readonly IDb2SessionFactory _db2Factory;
    private readonly ConfigReader _configReader;
    private readonly AppConfig _appConfig;
    private string? _lastRunId;
    private bool _lastRunSucceeded;

    public RunService(
        IRunEventBroadcaster broadcaster,
        ILogger<RunService> logger,
        IOutputWriter outputWriter,
        ISqlModifier sqlModifier,
        IDb2SessionFactory db2Factory,
        ConfigReader configReader,
        AppConfig appConfig)
    {
        _broadcaster = broadcaster;
        _logger = logger;
        _outputWriter = outputWriter;
        _sqlModifier = sqlModifier;
        _db2Factory = db2Factory;
        _configReader = configReader;
        _appConfig = appConfig;
    }

    public async Task<Run> StartRunAsync(UserContext userContext, RunSelection selection, CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid().ToString("N");
        _logger.LogInformation(
            "Run {RunId}: start requested user={Login} db={DatabaseId} manager={ManagerId} stream={StreamId}",
            runId,
            userContext.Login,
            userContext.DatabaseId,
            userContext.ManagerId,
            userContext.StreamId);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runCts[runId] = cts;
        var runToken = cts.Token;

        _logger.LogInformation("Run {RunId}: reading scripts.json and columns.json", runId);
        var scriptsConfig = await _configReader.ReadAsync<ScriptsConfig>("scripts.json", runToken);
        var columnsConfig = await _configReader.ReadAsync<ColumnsConfig>("columns.json", runToken);

        _logger.LogInformation(
            "Run {RunId}: output cleanup policy={Policy} root={Root}",
            runId,
            _appConfig.Output.CleanupPolicy,
            _appConfig.Output.RootPath);

        OutputCleanup.Apply(
            _appConfig.Output.RootPath,
            _appConfig.Output.CleanupPolicy,
            _lastRunId,
            _lastRunSucceeded);

        var run = new Run
        {
            RunId = runId,
            UserContext = userContext,
            Selection = selection,
            Status = RunStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var g in selection.Groups.Where(x => x.Enabled))
        {
            run.GroupStatuses[g.GroupId] = GroupStatus.Queued;
            run.ScriptStatuses[g.GroupId] = new Dictionary<string, ScriptStatus>();
            foreach (var s in g.Scripts.Where(x => x.Enabled))
                run.ScriptStatuses[g.GroupId][s.ScriptId] = ScriptStatus.Queued;
        }
        _runs[runId] = run;

        _logger.LogInformation(
            "Run {RunId}: queued enabledGroups={Groups} laneCount={LaneCount}",
            runId,
            selection.Groups.Count(g => g.Enabled),
            _appConfig.Execution.LaneCount);

        await _broadcaster.BroadcastRunUpdatedAsync(runId, RunStatus.Queued, runToken);

        var scriptsRoot = ResolveScriptsRoot();
        _logger.LogInformation("Run {RunId}: scriptsRoot={ScriptsRoot}", runId, scriptsRoot);
        var tasks = BuildVariantTasks(selection, scriptsConfig);
        _logger.LogInformation("Run {RunId}: tasks totalVariants={VariantCount}", runId, tasks.Count);
        var byLane = tasks.GroupBy(t => t.Lane).ToDictionary(g => g.Key, g => g.ToList());
        var laneCount = _appConfig.Execution.LaneCount;
        var output = _appConfig.Output;
        var encoding = Encoding.GetEncoding(output.Encoding);

        _ = Task.Run(async () =>
        {
            try
            {
                run.Status = RunStatus.Running;
                run.UpdatedAt = DateTime.UtcNow;
                await _broadcaster.BroadcastRunUpdatedAsync(runId, RunStatus.Running, runToken);
                _logger.LogInformation("Run {RunId}: running", runId);

                var variantStatuses = new Dictionary<string, Dictionary<string, List<ScriptStatus>>>();

                async Task RunLaneAsync(int lane)
                {
                    if (!byLane.TryGetValue(lane, out var list))
                        return;
                    _logger.LogInformation("Run {RunId}: lane {Lane} start tasks={Tasks}", runId, lane, list.Count);
                    foreach (var t in list)
                    {
                        runToken.ThrowIfCancellationRequested();
                        var gSel = selection.Groups.FirstOrDefault(x => x.GroupId == t.GroupId && x.Enabled);
                        var sSel = gSel?.Scripts.FirstOrDefault(x => x.ScriptId == t.ScriptId && x.Enabled);
                        if (gSel == null || sSel == null)
                            continue;

                        if (!variantStatuses.ContainsKey(t.GroupId))
                            variantStatuses[t.GroupId] = new Dictionary<string, List<ScriptStatus>>();
                        if (!variantStatuses[t.GroupId].ContainsKey(t.ScriptId))
                            variantStatuses[t.GroupId][t.ScriptId] = new List<ScriptStatus>();

                        var vIdx = t.VariantIndex + 1;
                        await _broadcaster.BroadcastScriptUpdatedAsync(runId, t.GroupId, t.ScriptId, ScriptStatus.Running, $"Executing variant {vIdx}/3", runToken);

                        _logger.LogInformation(
                            "Run {RunId}: execute group={GroupId} script={ScriptId} variant={VariantId} lane={Lane} idx={Idx}",
                            runId,
                            t.GroupId,
                            t.ScriptId,
                            t.VariantId,
                            lane,
                            vIdx);

                        var status = await ExecuteVariantAsync(runId, t, gSel, sSel, scriptsRoot, columnsConfig, output, encoding, runToken);
                        variantStatuses[t.GroupId][t.ScriptId].Add(status);
                        _logger.LogInformation(
                            "Run {RunId}: executed group={GroupId} script={ScriptId} variant={VariantId} status={Status}",
                            runId,
                            t.GroupId,
                            t.ScriptId,
                            t.VariantId,
                            status);

                        if (variantStatuses[t.GroupId][t.ScriptId].Count == 3)
                        {
                            var agg = AggregateScriptStatus(variantStatuses[t.GroupId][t.ScriptId]);
                            run.ScriptStatuses[t.GroupId][t.ScriptId] = agg;
                            run.UpdatedAt = DateTime.UtcNow;
                            await _broadcaster.BroadcastScriptUpdatedAsync(runId, t.GroupId, t.ScriptId, agg, null, runToken);
                            _logger.LogInformation(
                                "Run {RunId}: script aggregate group={GroupId} script={ScriptId} status={Status}",
                                runId,
                                t.GroupId,
                                t.ScriptId,
                                agg);
                        }
                    }
                    _logger.LogInformation("Run {RunId}: lane {Lane} done", runId, lane);
                }

                var laneTasks = Enumerable.Range(0, laneCount).Select(l => RunLaneAsync(l));
                await Task.WhenAll(laneTasks);

                foreach (var g in run.GroupStatuses.Keys.ToList())
                    run.GroupStatuses[g] = AggregateGroupStatus(run.ScriptStatuses[g]);
                run.Status = AggregateRunStatus(run.GroupStatuses.Values);
                run.UpdatedAt = DateTime.UtcNow;

                foreach (var g in selection.Groups.Where(x => x.Enabled))
                    await _broadcaster.BroadcastGroupUpdatedAsync(runId, g.GroupId, run.GroupStatuses[g.GroupId], runToken);

                _lastRunId = runId;
                _lastRunSucceeded = run.Status == RunStatus.Success;
                await _broadcaster.BroadcastRunUpdatedAsync(runId, run.Status, runToken);
                _logger.LogInformation("Run {RunId}: finished status={Status} lastRunSucceeded={Succeeded}", runId, run.Status, _lastRunSucceeded);
            }
            catch (OperationCanceledException)
            {
                run.Status = RunStatus.Cancelled;
                run.UpdatedAt = DateTime.UtcNow;
                foreach (var g in run.GroupStatuses.Keys.ToList())
                    run.GroupStatuses[g] = GroupStatus.Cancelled;
                foreach (var gs in run.ScriptStatuses.Values)
                    foreach (var s in gs.Keys.ToList())
                        gs[s] = ScriptStatus.Cancelled;
                _lastRunId = runId;
                _lastRunSucceeded = false;
                await _broadcaster.BroadcastRunUpdatedAsync(runId, RunStatus.Cancelled, CancellationToken.None);
                _logger.LogWarning("Run {RunId}: cancelled", runId);
            }
            catch (Exception ex)
            {
                run.Status = RunStatus.Failed;
                run.UpdatedAt = DateTime.UtcNow;
                _lastRunId = runId;
                _lastRunSucceeded = false;
                _logger.LogError(ex, "Run {RunId}: failed with unhandled exception", runId);
                await _broadcaster.BroadcastRunUpdatedAsync(runId, RunStatus.Failed, CancellationToken.None);
            }
            finally
            {
                _runCts.TryRemove(runId, out _);
            }
        }, runToken);

        return run;
    }

    public Task<Run?> GetRunAsync(string runId, CancellationToken cancellationToken)
    {
        _runs.TryGetValue(runId, out var run);
        return Task.FromResult(run);
    }

    public Task<bool> CancelRunAsync(string runId, CancellationToken cancellationToken)
    {
        if (!_runs.TryGetValue(runId, out var run))
            return Task.FromResult(false);
        if (run.Status is RunStatus.Success or RunStatus.Failed or RunStatus.Cancelled)
            return Task.FromResult(false);
        if (!_runCts.TryGetValue(runId, out var cts))
            return Task.FromResult(false);
        _logger.LogInformation("Run {RunId}: cancel requested currentStatus={Status}", runId, run.Status);
        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException) { }

        run.Status = RunStatus.Cancelled;
        run.UpdatedAt = DateTime.UtcNow;
        foreach (var g in run.GroupStatuses.Keys.ToList())
            run.GroupStatuses[g] = GroupStatus.Cancelled;
        foreach (var gs in run.ScriptStatuses.Values)
            foreach (var s in gs.Keys.ToList())
                gs[s] = ScriptStatus.Cancelled;

        _ = Task.Run(async () =>
            await _broadcaster.BroadcastRunUpdatedAsync(runId, RunStatus.Cancelled, cancellationToken), cancellationToken);

        return Task.FromResult(true);
    }

    private string ResolveScriptsRoot()
    {
        var root = _appConfig.Output.ScriptsRoot;
        return Path.IsPathRooted(root)
            ? root
            : Path.GetFullPath(Path.Combine(_configReader.ConfigPath, "..", root));
    }

    private static List<VariantTask> BuildVariantTasks(RunSelection selection, ScriptsConfig scriptsConfig)
    {
        var list = new List<VariantTask>();
        foreach (var g in selection.Groups.Where(x => x.Enabled))
        {
            var gc = scriptsConfig.Groups.FirstOrDefault(c => c.Id == g.GroupId);
            if (gc == null) continue;
            foreach (var s in g.Scripts.Where(x => x.Enabled))
            {
                var sc = gc.Scripts.FirstOrDefault(c => c.Id == s.ScriptId);
                if (sc == null) continue;
                for (var v = 0; v < sc.Variants.Count; v++)
                {
                    list.Add(new VariantTask(
                        g.GroupId,
                        s.ScriptId,
                        sc.Variants[v].Id,
                        sc.Variants[v].SqlFile,
                        sc.ExecutionLane,
                        sc.ColumnsProfileId,
                        v));
                }
            }
        }
        return list;
    }

    private async Task<ScriptStatus> ExecuteVariantAsync(
        string runId,
        VariantTask t,
        GroupSelection gSel,
        ScriptSelection sSel,
        string scriptsRoot,
        ColumnsConfig columnsConfig,
        OutputConfig output,
        Encoding encoding,
        CancellationToken ct)
    {
        var sqlPath = Path.Combine(scriptsRoot, t.SqlFile);
        if (!File.Exists(sqlPath))
        {
            _logger.LogError("Run {RunId}: sql file not found path={SqlPath}", runId, sqlPath);
            return ScriptStatus.Failed;
        }

        var sql = await File.ReadAllTextAsync(sqlPath, ct);
        if (sSel.ExportMode == ExportMode.CustomColumns && sSel.SelectedColumnItemIds.Count > 0)
        {
            var profile = columnsConfig.Profiles.FirstOrDefault(p => p.Id == t.ColumnsProfileId);
            if (profile != null)
            {
                try
                {
                    var ctx = new SqlModifierContext
                    {
                        SelectedColumnItemIds = sSel.SelectedColumnItemIds,
                        IdToExpression = profile.Items.ToDictionary(i => i.Id, i => i.Expression),
                        Delimiter = columnsConfig.Serialization.Delimiter,
                        Escape = new EscapeRules
                        {
                            Backslash = columnsConfig.Serialization.Escape.Backslash,
                            Pipe = columnsConfig.Serialization.Escape.Pipe,
                            Cr = columnsConfig.Serialization.Escape.Cr,
                            Lf = columnsConfig.Serialization.Escape.Lf
                        }
                    };
                    sql = _sqlModifier.Modify(sql, ctx);
                }
                catch (InvalidOperationException)
                {
                    _logger.LogError(
                        "Run {RunId}: sql modify failed group={GroupId} script={ScriptId} variant={VariantId}",
                        runId,
                        t.GroupId,
                        t.ScriptId,
                        t.VariantId);
                    return ScriptStatus.Failed;
                }
            }
        }

        await using var session = await _db2Factory.CreateAsync("", "", "", "", ct);
        var lines = new List<string>();
        await foreach (var line in session.ExecuteQueryAsync(sql, ct))
            lines.Add(line);

        if (lines.Count == 0)
        {
            _logger.LogInformation(
                "Run {RunId}: no data group={GroupId} script={ScriptId} variant={VariantId}",
                runId,
                t.GroupId,
                t.ScriptId,
                t.VariantId);
            return ScriptStatus.NoData;
        }

        var basePath = Path.Combine(output.RootPath, runId, t.GroupId, t.ScriptId, t.VariantId);
        _logger.LogInformation(
            "Run {RunId}: writing output basePath={BasePath} lines={LineCount} maxFileBytes={MaxFileBytes}",
            runId,
            basePath,
            lines.Count,
            output.MaxFileBytes);
        await _outputWriter.WriteAsync(
            basePath,
            ToAsyncEnumerable(lines),
            encoding,
            output.MaxFileBytes,
            output.AllowOversizeSingleLine,
            ct);

        return ScriptStatus.Success;
    }

    private static async IAsyncEnumerable<string> ToAsyncEnumerable(List<string> list)
    {
        await Task.Yield();
        foreach (var s in list)
            yield return s;
    }

    private static ScriptStatus AggregateScriptStatus(List<ScriptStatus> variantStatuses)
    {
        if (variantStatuses.Any(s => s == ScriptStatus.Failed)) return ScriptStatus.Failed;
        if (variantStatuses.All(s => s == ScriptStatus.NoData)) return ScriptStatus.NoData;
        if (variantStatuses.All(s => s == ScriptStatus.Success || s == ScriptStatus.NoData))
            return variantStatuses.Any(s => s == ScriptStatus.Success) ? ScriptStatus.Success : ScriptStatus.NoData;
        if (variantStatuses.Any(s => s == ScriptStatus.Cancelled)) return ScriptStatus.Cancelled;
        return ScriptStatus.Running;
    }

    private static GroupStatus AggregateGroupStatus(Dictionary<string, ScriptStatus> scriptStatuses)
    {
        var list = scriptStatuses.Values.ToList();
        if (list.Any(s => s == ScriptStatus.Failed)) return GroupStatus.Failed;
        if (list.All(s => s == ScriptStatus.NoData)) return GroupStatus.NoData;
        if (list.All(s => s == ScriptStatus.Success || s == ScriptStatus.NoData))
            return list.Any(s => s == ScriptStatus.Success) ? GroupStatus.Success : GroupStatus.NoData;
        if (list.Any(s => s == ScriptStatus.Cancelled)) return GroupStatus.Cancelled;
        if (list.Any(s => s == ScriptStatus.Running)) return GroupStatus.Running;
        return GroupStatus.Queued;
    }

    private static RunStatus AggregateRunStatus(IEnumerable<GroupStatus> groupStatuses)
    {
        var list = groupStatuses.ToList();
        if (list.Any(s => s == GroupStatus.Failed)) return RunStatus.Failed;
        if (list.All(s => s == GroupStatus.NoData)) return RunStatus.NoData;
        if (list.All(s => s == GroupStatus.Success || s == GroupStatus.NoData))
            return list.Any(s => s == GroupStatus.Success) ? RunStatus.Success : RunStatus.NoData;
        if (list.Any(s => s == GroupStatus.Cancelled)) return RunStatus.Cancelled;
        return RunStatus.Success;
    }

    private sealed record VariantTask(
        string GroupId,
        string ScriptId,
        string VariantId,
        string SqlFile,
        int Lane,
        string? ColumnsProfileId,
        int VariantIndex);
}
