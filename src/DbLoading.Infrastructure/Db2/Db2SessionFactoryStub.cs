using DbLoading.Application.Db2;
using DbLoading.Database;
using DbLoading.Database.Mock;

namespace DbLoading.Infrastructure.Db2;

[Obsolete("Use MockDbConnectionFactory from DbLoading.Database.Mock instead")]
public sealed class Db2SessionFactoryStub : IDb2SessionFactory
{
    private readonly IDbConnectionFactory _connectionFactory = new MockDbConnectionFactory();

    public async Task<IDb2Session> CreateAsync(string server, string database, string uid, string pwd, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateAsync(server, database, uid, pwd, cancellationToken);
        await connection.ConnectAsync(cancellationToken);
        return new Db2SessionStub(connection);
    }
}
