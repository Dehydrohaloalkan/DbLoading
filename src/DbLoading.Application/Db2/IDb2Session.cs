using DbLoading.Database;

namespace DbLoading.Application.Db2;

[Obsolete("Use IDbConnection from DbLoading.Database instead")]
public interface IDb2Session : IAsyncDisposable
{
    IAsyncEnumerable<string> ExecuteQueryAsync(string sql, CancellationToken cancellationToken = default);
}

public static class DbConnectionExtensions
{
    public static IAsyncEnumerable<string> ExecuteQueryAsync(
        this IDbConnection connection, 
        string sql, 
        CancellationToken cancellationToken = default)
    {
        return connection.ExecuteAsync(sql, cancellationToken);
    }
}
