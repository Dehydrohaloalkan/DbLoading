using System.Buffers;
using System.Text;
using DbLoading.Application.Export;

namespace DbLoading.Infrastructure.Export;

public sealed class FileSlicerWriter : IOutputWriter
{
    public async Task WriteAsync(
        string basePath,
        IAsyncEnumerable<string> lines,
        Encoding encoding,
        long maxFileBytes,
        bool allowOversizeSingleLine,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(basePath);
        var newline = encoding.GetBytes(Environment.NewLine);
        var newlineLen = newline.Length;
        var partIndex = 0;
        Stream? currentStream = null;
        long currentBytes = 0;
        byte[]? lineBuffer = null;

        await foreach (var line in lines.WithCancellation(cancellationToken))
        {
            var lineBytes = encoding.GetByteCount(line);
            var totalForLine = lineBytes + newlineLen;

            if (currentBytes + totalForLine > maxFileBytes && currentBytes > 0)
            {
                if (currentStream != null)
                {
                    await currentStream.DisposeAsync();
                    currentStream = null;
                }
                currentBytes = 0;
            }

            if (currentStream == null)
            {
                partIndex++;
                var path = Path.Combine(basePath, $"part-{partIndex:D4}.txt");
                currentStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
            }

            var maxBuffer = lineBytes + newlineLen;
            lineBuffer = ArrayPool<byte>.Shared.Rent(maxBuffer);
            try
            {
                var encoded = encoding.GetBytes(line, 0, line.Length, lineBuffer, 0);
                Buffer.BlockCopy(newline, 0, lineBuffer, encoded, newlineLen);
                await currentStream.WriteAsync(lineBuffer.AsMemory(0, encoded + newlineLen), cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(lineBuffer);
            }
            currentBytes += totalForLine;
        }

        if (currentStream != null)
            await currentStream.DisposeAsync();
    }
}
