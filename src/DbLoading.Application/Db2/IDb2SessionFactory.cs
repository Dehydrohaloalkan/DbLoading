namespace DbLoading.Application.Db2;

public interface IDb2SessionFactory
{
    Task<IDb2Session> CreateAsync(string server, string database, string uid, string pwd, CancellationToken cancellationToken = default);
}
