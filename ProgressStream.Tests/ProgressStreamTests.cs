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
    }
}
