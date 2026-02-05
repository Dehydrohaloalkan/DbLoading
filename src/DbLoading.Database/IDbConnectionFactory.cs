namespace DbLoading.Database;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateAsync(
        string server,
        string database,
        string uid,
        string pwd,
        CancellationToken cancellationToken = default);
}
