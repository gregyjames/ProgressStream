using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProgressStream;

public class ProgressStream: Stream
{
    private readonly Stream _innerStream;
    private readonly IProgress<int>? _readProgress;
    private readonly IProgress<int>? _writeProgress;

    public ProgressStream(Stream stream, IProgress<int>? readProgress = null, IProgress<int>? writeProgress = null)
    {
        _innerStream = stream;
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

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _innerStream.WriteAsync(buffer, offset, count, cancellationToken).ContinueWith(_ =>
        {
            _writeProgress?.Report(count);
        }, cancellationToken);
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