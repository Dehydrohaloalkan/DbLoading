using System.Runtime.CompilerServices;

namespace DbLoading.Database.Mock;

public sealed class MockDbConnection : IDbConnection
{
    private readonly string _uid;
    private readonly string _pwd;
    private bool _connected;

    public MockDbConnection(string uid, string pwd)
    {
        _uid = uid;
        _pwd = pwd;
    }

    public bool IsConnected => _connected;

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (string.IsNullOrWhiteSpace(_uid) || string.IsNullOrWhiteSpace(_pwd))
        {
            throw new DbConnectionException("Invalid credentials: username and password are required");
        }

        _connected = true;
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<string> ExecuteAsync(
        string sql, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_connected)
        {
            throw new DbConnectionException("Not connected. Call ConnectAsync first.");
        }

        await Task.Yield();
        
        for (var i = 0; i < 5; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return $"mock_row_{i + 1}";
        }
    }

    public ValueTask DisposeAsync()
    {
        _connected = false;
        return ValueTask.CompletedTask;
    }
}
