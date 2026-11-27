using ProgressStream.Tests.TestDoubles;

namespace ProgressStream.Tests;

public class StreamProgressTests
{
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
}
