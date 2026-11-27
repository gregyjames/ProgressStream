namespace ProgressStream.Tests.TestDoubles;

internal sealed class FakeLargeStream(long length, bool canSeek) : Stream
{
    private long _length = length;
    private long _position;

    public override bool CanRead => true;
    public override bool CanSeek => canSeek;
    public override bool CanWrite => true;
    public override long Length => canSeek ? _length : throw new NotSupportedException();
    public override long Position
    {
        get => _position;
        set => _position = value;
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count) => AdvanceRead(count);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        Task.FromResult(AdvanceRead(count));

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(AdvanceRead(buffer.Length));

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => _length = value;

    public override void Write(byte[] buffer, int offset, int count) => AdvanceWrite(count);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        AdvanceWrite(count);
        return Task.CompletedTask;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        AdvanceWrite(buffer.Length);
        return ValueTask.CompletedTask;
    }

    private int AdvanceRead(int count)
    {
        long remaining = _length - _position;
        if (remaining <= 0)
        {
            return 0;
        }

        int toRead = (int)Math.Min(count, remaining);
        _position += toRead;
        return toRead;
    }

    private void AdvanceWrite(int count)
    {
        _position += count;
        if (_position > _length)
        {
            _length = _position;
        }
    }
}
