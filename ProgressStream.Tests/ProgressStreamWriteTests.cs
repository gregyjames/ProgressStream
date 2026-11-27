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
}
