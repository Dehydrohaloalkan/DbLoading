namespace DbLoading.Application.Export;

public interface IOutputWriter
{
    Task WriteAsync(
        string basePath,
        IAsyncEnumerable<string> lines,
        System.Text.Encoding encoding,
        long maxFileBytes,
        bool allowOversizeSingleLine,
        CancellationToken cancellationToken = default);
}
