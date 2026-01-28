using DbLoading.Application.Db2;

namespace DbLoading.Infrastructure.Db2;

public sealed class Db2SessionFactoryStub : IDb2SessionFactory
{
    public Task<IDb2Session> CreateAsync(string server, string database, string uid, string pwd, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IDb2Session>(new Db2SessionStub());
    }
}
