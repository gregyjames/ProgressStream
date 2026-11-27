using ProgressStream.Tests.TestDoubles;

namespace ProgressStream.Tests;

public class ProgressStreamWriteTests
{
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

        bytesReadOverall.Should().Be((int)inputStream.Length);
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

        bytesReadOverall.Should().Be((int)inputStream.Length);
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

        bytesReadOverall.Should().Be((int)inputStream.Length);
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

        stream.TotalBytesWritten.Should().Be(100L);
        stream.TotalBytesRead.Should().Be(0L);
        reports.Should().HaveCount(4);
        reports[0].Should().Be(new StreamProgress(25, 25, 100));
        reports[0].Percentage.Should().Be(25.0);
        reports[3].Should().Be(new StreamProgress(25, 100, 100));
        reports[3].Percentage.Should().Be(100.0);
    }
}
