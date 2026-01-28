namespace DbLoading.Application.Db2;

public interface IDb2Session : IAsyncDisposable
{
    IAsyncEnumerable<string> ExecuteQueryAsync(string sql, CancellationToken cancellationToken = default);
}
