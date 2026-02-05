namespace DbLoading.Database.Mock;

public sealed class MockDbConnectionFactory : IDbConnectionFactory
{
    public Task<IDbConnection> CreateAsync(
        string server,
        string database,
        string uid,
        string pwd,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IDbConnection>(new MockDbConnection(uid, pwd));
    }
}
