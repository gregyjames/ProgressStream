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

        bytesReadOverall.Should().Be((int)inputStream.Length);
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

        bytesReadOverall.Should().Be((int)inputStream.Length);
    }

    [Fact]
    public void StreamProgress_ReadMultiChunk_TracksCumulativeAndPercentage()
    {
        var reports = new List<StreamProgress>();
        var readProgress = new UnitTestProgress<StreamProgress>(reports.Add);
        using var inner = new MemoryStream(new byte[100]);
        using var stream = new ProgressStream(inner, readProgress);

        var buffer = new byte[40];
        stream.Read(buffer, 0, buffer.Length).Should().Be(40);
        stream.Read(buffer, 0, buffer.Length).Should().Be(40);
        stream.Read(buffer, 0, buffer.Length).Should().Be(20);
        stream.Read(buffer, 0, buffer.Length).Should().Be(0);

        stream.TotalBytesRead.Should().Be(100L);
        stream.TotalBytesWritten.Should().Be(0L);

        reports.Should().BeEquivalentTo([
            new StreamProgress(40, 40, 100),
            new StreamProgress(40, 80, 100),
            new StreamProgress(20, 100, 100)
        ], options => options.WithStrictOrdering());

        reports[0].Percentage.Should().Be(40.0);
        reports[1].Percentage.Should().Be(80.0);
        reports[2].Percentage.Should().Be(100.0);
    }

    [Fact]
    public async Task StreamProgress_NonSeekableWithoutTotalLength_ReturnsNullLengthAndPercentage()
    {
        var reports = new List<StreamProgress>();
        var readProgress = new UnitTestProgress<StreamProgress>(reports.Add);
        await using var inner = new FakeLargeStream(200, canSeek: false);
        await using var stream = new ProgressStream(inner, readProgress);

        var buffer = new byte[100];
        (await stream.ReadAsync(buffer, TestContext.Current.CancellationToken)).Should().Be(100);
        (await stream.ReadAsync(buffer, TestContext.Current.CancellationToken)).Should().Be(100);

        reports.Should().HaveCount(2);
        reports[0].TotalLength.Should().BeNull();
        reports[0].Percentage.Should().BeNull();
        reports[0].TotalBytesTransferred.Should().Be(100L);
        reports[1].TotalBytesTransferred.Should().Be(200L);
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
        stream.Read(buffer, 0, buffer.Length).Should().Be(10);

        var report = reports.Should().ContainSingle().Subject;
        report.BytesTransferred.Should().Be(10L);
        report.TotalBytesTransferred.Should().Be(10L);
        report.TotalLength.Should().BeNull();
        report.Percentage.Should().BeNull();
    }
}
