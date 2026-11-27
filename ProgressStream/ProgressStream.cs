using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProgressStream
{
#pragma warning disable CA1844 // Provide memory-based overrides of async methods when subclassing 'Stream'
    public class ProgressStream : Stream
#pragma warning restore CA1844 // Provide memory-based overrides of async methods when subclassing 'Stream'
    {
        private readonly Stream _innerStream;
        private readonly IProgress<int>? _readProgress;
        private readonly IProgress<int>? _writeProgress;
        private readonly IProgress<StreamProgress>? _readStreamProgress;
        private readonly IProgress<StreamProgress>? _writeStreamProgress;
        private readonly long? _totalLength;
        private long _totalBytesRead;
        private long _totalBytesWritten;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressStream"/> class. The wrapper for a stream that tracks byte counts.
        /// </summary>
        /// <param name="stream">The underlying stream that is being written to or read from.</param>
        public ProgressStream(Stream stream)
            : this(stream, (IProgress<StreamProgress>?)null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressStream"/> class. The wrapper for a stream that provides progress.
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressStream"/> class with cumulative <see cref="StreamProgress"/> reporting.
        /// </summary>
        /// <param name="stream">The underlying stream that is being written to or read from.</param>
        /// <param name="readProgress">Progress reporter receiving cumulative read progress.</param>
        /// <param name="writeProgress">Progress reporter receiving cumulative write progress.</param>
        /// <param name="totalLength">Optional total length of the stream in bytes, useful for non-seekable streams such as HTTP responses.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="totalLength"/> is negative.</exception>
        public ProgressStream(
            Stream stream,
            IProgress<StreamProgress>? readProgress = null,
            IProgress<StreamProgress>? writeProgress = null,
            long? totalLength = null)
        {
            _innerStream = stream ?? throw new ArgumentNullException(nameof(stream));

            if (totalLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalLength), "Total length cannot be negative.");
            }

            _readStreamProgress = readProgress;
            _writeStreamProgress = writeProgress;
            _totalLength = totalLength;
        }

        /// <summary>
        /// Gets the cumulative number of bytes read from the stream.
        /// </summary>
        public long TotalBytesRead => Interlocked.Read(ref _totalBytesRead);

        /// <summary>
        /// Gets the cumulative number of bytes written to the stream.
        /// </summary>
        public long TotalBytesWritten => Interlocked.Read(ref _totalBytesWritten);

        /// <inheritdoc />
        public override void Flush()
        {
            _innerStream.Flush();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArgs(buffer, offset, count);

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

            ReportRead(totalBytesRead);

            return totalBytesRead;
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArgs(buffer, offset, count);

            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
#if NET || NETSTANDARD2_1
                int bytesRead = await _innerStream.ReadAsync(
                    buffer.AsMemory(offset + totalBytesRead, count - totalBytesRead),
                    cancellationToken);
#else
                int bytesRead = await _innerStream.ReadAsync(buffer, offset + totalBytesRead, count - totalBytesRead, cancellationToken);
#endif
                if (bytesRead == 0)
                {
                    break; // end of stream
                }

                totalBytesRead += bytesRead;
            }

            ReportRead(totalBytesRead);

            return totalBytesRead;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArgs(buffer, offset, count);

            _innerStream.Write(buffer, offset, count);
            ReportWrite(count);
        }

        /// <inheritdoc />
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArgs(buffer, offset, count);

#if NET || NETSTANDARD2_1
            await _innerStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
#else
            await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
#endif
            ReportWrite(count);
        }

        /// <inheritdoc />
        public override bool CanRead => _innerStream.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => _innerStream.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => _innerStream.CanWrite;

        /// <inheritdoc />
        public override long Length => _innerStream.Length;

        /// <inheritdoc />
        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerStream.Dispose();
            }

            base.Dispose(disposing);
        }

        private void ReportRead(int bytesRead) =>
            ReportProgress(bytesRead, ref _totalBytesRead, _readProgress, _readStreamProgress);

        private void ReportWrite(int bytesWritten) =>
            ReportProgress(bytesWritten, ref _totalBytesWritten, _writeProgress, _writeStreamProgress);

        private void ReportProgress(
            int bytesTransferred,
            ref long totalBytes,
            IProgress<int>? intProgress,
            IProgress<StreamProgress>? streamProgress)
        {
            if (bytesTransferred > 0)
            {
                long total = Interlocked.Add(ref totalBytes, bytesTransferred);
                intProgress?.Report(bytesTransferred);
                streamProgress?.Report(new StreamProgress(bytesTransferred, total, ResolveTotalLength()));
                return;
            }

            intProgress?.Report(bytesTransferred);
        }

        private long? ResolveTotalLength()
        {
            if (_totalLength.HasValue)
            {
                return _totalLength;
            }

            if (!_innerStream.CanSeek)
            {
                return null;
            }

            try
            {
                return _innerStream.Length;
            }
            catch (Exception ex) when (ex is NotSupportedException || ex is ObjectDisposedException)
            {
                return null;
            }
        }

        /// <summary>
        /// Validates the buffer, offset, and count arguments for read/write calls.
        /// </summary>
        /// <param name="buffer">The buffer to read or write.</param>
        /// <param name="offset">The offset within <paramref name="buffer"/>.</param>
        /// <param name="count">The number of bytes to read or write.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="buffer"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/> or <paramref name="count"/> are invalid.</exception>
        /// <exception cref="ArgumentException">Thrown if the sum of <paramref name="offset"/> and <paramref name="count"/> are invalid.</exception>
        private static void ValidateBufferArgs(byte[] buffer, int offset, int count)
        {
#if NET
            ArgumentNullException.ThrowIfNull(buffer);
#else
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
#endif

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative.");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
            }

            if (offset + count > buffer.Length)
            {
                throw new ArgumentException("The sum of offset and count is greater than the buffer length.", nameof(buffer));
            }
        }
    }
}
