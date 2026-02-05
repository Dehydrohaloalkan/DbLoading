namespace DbLoading.Database;

public interface IDbConnection : IAsyncDisposable
{
    bool IsConnected { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> ExecuteAsync(string sql, CancellationToken cancellationToken = default);
}
