using DbLoading.Application.Db2;
using DbLoading.Database;

namespace DbLoading.Infrastructure.Db2;

[Obsolete("Use MockDbConnection from DbLoading.Database.Mock instead")]
public sealed class Db2SessionStub : IDb2Session
{
    private readonly IDbConnection _connection;

    public Db2SessionStub(IDbConnection connection)
    {
        _connection = connection;
    }

    public IAsyncEnumerable<string> ExecuteQueryAsync(string sql, CancellationToken cancellationToken = default)
    {
        return _connection.ExecuteAsync(sql, cancellationToken);
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
