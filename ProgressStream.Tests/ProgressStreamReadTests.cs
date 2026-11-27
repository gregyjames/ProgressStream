using ProgressStream.Tests.TestDoubles;

namespace ProgressStream.Tests;

public class ProgressStreamReadTests
{
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
}
