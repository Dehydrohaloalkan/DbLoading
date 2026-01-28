using System.Runtime.CompilerServices;
using DbLoading.Application.Db2;

namespace DbLoading.Infrastructure.Db2;

public sealed class Db2SessionStub : IDb2Session
{
    public async IAsyncEnumerable<string> ExecuteQueryAsync(string sql, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        for (var i = 0; i < 5; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return $"stub_line_{i + 1}";
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
