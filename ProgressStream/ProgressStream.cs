using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;

namespace ProgressStream;

public class ProgressStream: Stream
{
    private readonly Stream _innerStream;
    private readonly IProgress<int>? _readProgress;
    private readonly IProgress<int>? _writeProgress;

    /// <summary>
    /// The wrapper for a stream that provides progress.
    /// </summary>
    /// <param name="stream">The underlying stream that is being written to or read from.</param>
    /// <param name="readProgress">IProgress for read progress.</param>
    /// <param name="writeProgress">IProgress for write progress.</param>
    public ProgressStream(Stream stream, IProgress<int>? readProgress = null, IProgress<int>? writeProgress = null)
    {
        _innerStream = stream ?? throw new ArgumentNullException(nameof(stream));
        _readProgress = readProgress;
        _writeProgress = writeProgress;
    }
    
    public override void Flush()
    {
        _innerStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalBytesRead = 0;
        while (totalBytesRead < count)
        {
            int bytesRead = _innerStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
            if (bytesRead == 0)
            {
                break; // end of stream
            }
            totalBytesRead += bytesRead;
        }
        _readProgress?.Report(totalBytesRead);
        return totalBytesRead;
    }
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int totalBytesRead = 0;
        while (totalBytesRead < count)
        {
            int bytesRead = await _innerStream.ReadAsync(buffer, offset + totalBytesRead, count - totalBytesRead, cancellationToken);
            if (bytesRead == 0)
            {
                break; // end of stream
            }
            totalBytesRead += bytesRead;
            _readProgress?.Report(bytesRead);
        }
        return totalBytesRead;
    }


    public override long Seek(long offset, SeekOrigin origin)
    {
        return _innerStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _innerStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _innerStream.Write(buffer, offset, count);
        _writeProgress?.Report(count);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        _writeProgress?.Report(count);
    }

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;
    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerStream.Dispose();
        }

        base.Dispose(disposing);
    }
}