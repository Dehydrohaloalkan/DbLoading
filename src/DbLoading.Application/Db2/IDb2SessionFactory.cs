using DbLoading.Database;

namespace DbLoading.Application.Db2;

[Obsolete("Use IDbConnectionFactory from DbLoading.Database instead")]
public interface IDb2SessionFactory
{
    Task<IDb2Session> CreateAsync(string server, string database, string uid, string pwd, CancellationToken cancellationToken = default);
}

public class DbConnectionFactoryAdapter : IDb2SessionFactory
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DbConnectionFactoryAdapter(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IDb2Session> CreateAsync(
        string server, 
        string database, 
        string uid, 
        string pwd, 
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateAsync(server, database, uid, pwd, cancellationToken);
        await connection.ConnectAsync(cancellationToken);
        return new DbConnectionSessionAdapter(connection);
    }
}

internal class DbConnectionSessionAdapter : IDb2Session
{
    private readonly IDbConnection _connection;

    public DbConnectionSessionAdapter(IDbConnection connection)
    {
        _connection = connection;
    }

    public IAsyncEnumerable<string> ExecuteQueryAsync(string sql, CancellationToken cancellationToken = default)
    {
        return _connection.ExecuteAsync(sql, cancellationToken);
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
