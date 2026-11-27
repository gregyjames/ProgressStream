using System;
using System.Text;

namespace ProgressStream.Tests
{
    public class ProgressStreamTests
    {
        private class UnitTestProgress<T>(Action<T> handler) : IProgress<T>
        {
            private readonly Action<T> _handler = handler;

            public void Report(T value)
            {
                _handler(value);
            }
        }

        [Fact]
        public async Task Properties()
        {
            Stream inputStream = new MemoryStream(new byte[10]);

            await using var stream = new ProgressStream(inputStream);

            Assert.Equal(inputStream.CanRead, stream.CanRead);
            Assert.Equal(inputStream.CanSeek, stream.CanSeek);
            Assert.Equal(inputStream.CanWrite, stream.CanWrite);
            Assert.Equal(inputStream.Length, stream.Length);

            inputStream.Position = 1;

            Assert.Equal(inputStream.Position, stream.Position);

            stream.Position = 2;

            Assert.Equal(inputStream.Position, stream.Position);
        }

        [Fact]
        public async Task Flush()
        {
            var buffer = new byte[1024760];

            Stream inputStream = new MemoryStream();

            int bytesReadOverall = 0;
            var writeProgress = new UnitTestProgress<int>(bytesRead =>
            {
                bytesReadOverall += bytesRead;
            });

            await using var stream = new MemoryStream(new byte[1_000_000]);
            await using var outputStream = new ProgressStream(inputStream, writeProgress: writeProgress);

            while (true)
            {
                var bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    break;
                }

                await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), TestContext.Current.CancellationToken);
            }

            await outputStream.FlushAsync(TestContext.Current.CancellationToken);

            Assert.Equal(inputStream.Length, bytesReadOverall);
        }

        [Fact]
        public async Task Seek()
        {
            Stream inputStream = new MemoryStream(new byte[10]);

            await using var stream = new ProgressStream(inputStream);

            stream.Seek(5, SeekOrigin.Begin);

            Assert.Equal(5, inputStream.Position);
            Assert.Equal(5, stream.Position);
        }

        [Fact]
        public async Task SetLength()
        {
            Stream inputStream = new MemoryStream(new byte[10]);

            await using var stream = new ProgressStream(inputStream);

            stream.SetLength(5);

            Assert.Equal(5, inputStream.Length);
            Assert.Equal(5, stream.Length);
        }

        [Fact]
        public async Task Read()
        {
            var buffer = new byte[1024760];

            Stream inputStream = new MemoryStream(new byte[1_000_000]);

            int bytesReadOverall = 0;
            var readProgress = new UnitTestProgress<int>(bytesRead =>
            {
                bytesReadOverall += bytesRead;
            });

            await using var stream = new ProgressStream(inputStream, readProgress);
            await using var outputStream = new MemoryStream();

            while (true)
            {
                var bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    break;
                }

                await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), TestContext.Current.CancellationToken);
            }

            Assert.Equal(inputStream.Length, bytesReadOverall);
        }

        [Fact]
        public async Task ReadAsync()
        {
            var buffer = new byte[1024760];

            Stream inputStream = new MemoryStream(new byte[1_000_000]);

            int bytesReadOverall = 0;
            var readProgress = new UnitTestProgress<int>(bytesRead =>
            {
                bytesReadOverall += bytesRead;
            });

            await using var stream = new ProgressStream(inputStream, readProgress);
            await using var outputStream = new MemoryStream();

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer, TestContext.Current.CancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), TestContext.Current.CancellationToken);
            }

            Assert.Equal(inputStream.Length, bytesReadOverall);
        }

        [Fact]
        public async Task Write()
        {
            var buffer = new byte[1024760];

            Stream inputStream = new MemoryStream();

            int bytesReadOverall = 0;
            var writeProgress = new UnitTestProgress<int>(bytesRead =>
            {
                bytesReadOverall += bytesRead;
            });

            await using var stream = new MemoryStream(new byte[1_000_000]);
            await using var outputStream = new ProgressStream(inputStream, writeProgress: writeProgress);

            while (true)
            {
                var bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    break;
                }

                outputStream.Write(buffer, 0, bytesRead);
            }

            Assert.Equal(inputStream.Length, bytesReadOverall);
        }

        [Fact]
        public async Task WriteAsync()
        {
            var buffer = new byte[1024760];

            Stream inputStream = new MemoryStream();

            int bytesReadOverall = 0;
            var writeProgress = new UnitTestProgress<int>(bytesRead =>
            {
                bytesReadOverall += bytesRead;
            });

            await using var stream = new MemoryStream(new byte[1_000_000]);
            await using var outputStream = new ProgressStream(inputStream, writeProgress: writeProgress);

            while (true)
            {
                var bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    break;
                }

                await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), TestContext.Current.CancellationToken);
            }

            Assert.Equal(inputStream.Length, bytesReadOverall);
        }

        [Fact]
        public void StreamProgress_ReadMultiChunk_TracksCumulativeAndPercentage()
        {
            var reports = new List<StreamProgress>();
            var readProgress = new UnitTestProgress<StreamProgress>(reports.Add);
            using var inner = new MemoryStream(new byte[100]);
            using var stream = new ProgressStream(inner, readProgress);

            var buffer = new byte[40];
            Assert.Equal(40, stream.Read(buffer, 0, buffer.Length));
            Assert.Equal(40, stream.Read(buffer, 0, buffer.Length));
            Assert.Equal(20, stream.Read(buffer, 0, buffer.Length));
            Assert.Equal(0, stream.Read(buffer, 0, buffer.Length));

            Assert.Equal(100L, stream.TotalBytesRead);
            Assert.Equal(0L, stream.TotalBytesWritten);
            Assert.Equal(3, reports.Count);

            Assert.Equal(new StreamProgress(40, 40, 100), reports[0]);
            Assert.Equal(40.0, reports[0].Percentage);
            Assert.Equal(new StreamProgress(40, 80, 100), reports[1]);
            Assert.Equal(80.0, reports[1].Percentage);
            Assert.Equal(new StreamProgress(20, 100, 100), reports[2]);
            Assert.Equal(100.0, reports[2].Percentage);
        }

        [Fact]
        public async Task StreamProgress_WriteAsyncWithExplicitTotalLength_TracksCumulativeAndPercentage()
        {
            var reports = new List<StreamProgress>();
            var writeProgress = new UnitTestProgress<StreamProgress>(reports.Add);
            await using var inner = new MemoryStream();
            await using var stream = new ProgressStream(inner, writeProgress: writeProgress, totalLength: 100);

            var chunk = new byte[25];
            for (int i = 0; i < 4; i++)
            {
                await stream.WriteAsync(chunk, TestContext.Current.CancellationToken);
            }

            Assert.Equal(100L, stream.TotalBytesWritten);
            Assert.Equal(0L, stream.TotalBytesRead);
            Assert.Equal(4, reports.Count);
            Assert.Equal(new StreamProgress(25, 25, 100), reports[0]);
            Assert.Equal(25.0, reports[0].Percentage);
            Assert.Equal(new StreamProgress(25, 100, 100), reports[3]);
            Assert.Equal(100.0, reports[3].Percentage);
        }

        [Fact]
        public async Task StreamProgress_NonSeekableWithoutTotalLength_ReturnsNullLengthAndPercentage()
        {
            var reports = new List<StreamProgress>();
            var readProgress = new UnitTestProgress<StreamProgress>(reports.Add);
            await using var inner = new FakeLargeStream(200, canSeek: false);
            await using var stream = new ProgressStream(inner, readProgress);

            var buffer = new byte[100];
            Assert.Equal(100, await stream.ReadAsync(buffer, TestContext.Current.CancellationToken));
            Assert.Equal(100, await stream.ReadAsync(buffer, TestContext.Current.CancellationToken));

            Assert.Equal(2, reports.Count);
            Assert.Null(reports[0].TotalLength);
            Assert.Null(reports[0].Percentage);
            Assert.Equal(100L, reports[0].TotalBytesTransferred);
            Assert.Equal(200L, reports[1].TotalBytesTransferred);
        }

        [Fact]
        public async Task StreamProgress_Exceeding2Gigabytes_DoesNotOverflow()
        {
            const long totalSize = 2_684_354_560L; // 2.5 GiB (> int.MaxValue)
            var buffer = new byte[4 * 1024 * 1024]; // 4 MiB reusable buffer

            var readReports = new List<StreamProgress>();
            var writeReports = new List<StreamProgress>();

            await using var source = new ProgressStream(
                new FakeLargeStream(totalSize, canSeek: false),
                readProgress: new UnitTestProgress<StreamProgress>(readReports.Add),
                totalLength: totalSize);

            await using var dest = new ProgressStream(
                new FakeLargeStream(0, canSeek: false),
                writeProgress: new UnitTestProgress<StreamProgress>(writeReports.Add),
                totalLength: totalSize);

            while (true)
            {
                int bytesRead = await source.ReadAsync(buffer, TestContext.Current.CancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                await dest.WriteAsync(buffer.AsMemory(0, bytesRead), TestContext.Current.CancellationToken);
            }

            Assert.Equal(totalSize, source.TotalBytesRead);
            Assert.Equal(totalSize, dest.TotalBytesWritten);
            Assert.True(source.TotalBytesRead > int.MaxValue);
            Assert.True(dest.TotalBytesWritten > int.MaxValue);

            Assert.Equal(totalSize, readReports[^1].TotalBytesTransferred);
            Assert.Equal(100.0, readReports[^1].Percentage);
            Assert.Equal(totalSize, writeReports[^1].TotalBytesTransferred);
            Assert.Equal(100.0, writeReports[^1].Percentage);
        }

        [Fact]
        public void Constructor_InvalidArguments_ThrowExpectedExceptions()
        {
            Assert.Throws<ArgumentNullException>(() => new ProgressStream(null!));
            Assert.Throws<ArgumentNullException>(() => new ProgressStream(null!, (IProgress<int>?)null));
            Assert.Throws<ArgumentNullException>(() => new ProgressStream(null!, (IProgress<StreamProgress>?)null));

            using var inner = new MemoryStream();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ProgressStream(inner, totalLength: -1));
        }

        [Fact]
        public void ReadAndWrite_InvalidBufferArguments_ThrowExpectedExceptions()
        {
            using var inner = new MemoryStream(new byte[10]);
            using var stream = new ProgressStream(inner);
            var buffer = new byte[10];

            Assert.Throws<ArgumentNullException>(() => stream.Read(null!, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(buffer, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(buffer, 0, -1));
            Assert.Throws<ArgumentException>(() => stream.Read(buffer, 5, 6));

            Assert.Throws<ArgumentNullException>(() => stream.Write(null!, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(buffer, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(buffer, 0, -1));
            Assert.Throws<ArgumentException>(() => stream.Write(buffer, 5, 6));
        }

        [Theory]
        [InlineData(typeof(NotSupportedException))]
        [InlineData(typeof(ObjectDisposedException))]
        public void StreamProgress_SeekableStreamWhoseLengthThrows_ReportsNullTotalLength(Type exceptionType)
        {
            Exception lengthException = exceptionType == typeof(ObjectDisposedException)
                ? new ObjectDisposedException("stream")
                : new NotSupportedException();

            var reports = new List<StreamProgress>();
            using var inner = new ThrowingLengthStream(lengthException);
            using var stream = new ProgressStream(inner, new UnitTestProgress<StreamProgress>(reports.Add));

            var buffer = new byte[10];
            Assert.Equal(10, stream.Read(buffer, 0, buffer.Length));

            Assert.Single(reports);
            Assert.Equal(10L, reports[0].BytesTransferred);
            Assert.Equal(10L, reports[0].TotalBytesTransferred);
            Assert.Null(reports[0].TotalLength);
            Assert.Null(reports[0].Percentage);
        }

        [Fact]
        public void StreamProgress_EqualityAndZeroLengthPercentage()
        {
            var a = new StreamProgress(10, 50, 100);
            var b = new StreamProgress(10, 50, 100);
            var c = new StreamProgress(10, 60, 100);
            var zeroLength = new StreamProgress(0, 0, 0);

            Assert.Equal(10L, a.BytesTransferred);
            Assert.Equal(50L, a.TotalBytesTransferred);
            Assert.Equal(100L, a.TotalLength);
            Assert.True(a == b);
            Assert.False(a != b);
            Assert.True(a.Equals((object)b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.True(a != c);
            Assert.False(a.Equals(null));
            Assert.Null(zeroLength.Percentage);
        }

        private sealed class ThrowingLengthStream(Exception lengthException) : Stream
        {
            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => throw lengthException;
            public override long Position
            {
                get => 0;
                set => throw new NotSupportedException();
            }

            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => count;
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        private sealed class FakeLargeStream(long length, bool canSeek) : Stream
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
    }
}
