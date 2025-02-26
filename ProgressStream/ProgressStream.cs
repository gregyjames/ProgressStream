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

            _readProgress?.Report(totalBytesRead);

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

            _readProgress?.Report(totalBytesRead);

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
            _writeProgress?.Report(count);
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
            _writeProgress?.Report(count);
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
