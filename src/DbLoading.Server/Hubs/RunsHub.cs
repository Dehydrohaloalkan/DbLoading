using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DbLoading.Server.Hubs;

[Authorize]
public class RunsHub : Hub
{
    private readonly ILogger<RunsHub> _logger;

    public RunsHub(ILogger<RunsHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("SignalR connected connectionId={ConnectionId} user={User}", Context.ConnectionId, Context.User?.Identity?.Name);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "SignalR disconnected with error connectionId={ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("SignalR disconnected connectionId={ConnectionId}", Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRunGroup(string runId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"runs:{runId}");
        _logger.LogInformation("SignalR join group runId={RunId} connectionId={ConnectionId}", runId, Context.ConnectionId);
    }

    public async Task LeaveRunGroup(string runId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"runs:{runId}");
        _logger.LogInformation("SignalR leave group runId={RunId} connectionId={ConnectionId}", runId, Context.ConnectionId);
    }
}