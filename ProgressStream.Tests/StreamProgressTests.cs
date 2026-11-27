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

        source.TotalBytesRead.Should().Be(totalSize).And.BeGreaterThan(int.MaxValue);
        dest.TotalBytesWritten.Should().Be(totalSize).And.BeGreaterThan(int.MaxValue);

        readReports[^1].TotalBytesTransferred.Should().Be(totalSize);
        readReports[^1].Percentage.Should().Be(100.0);
        writeReports[^1].TotalBytesTransferred.Should().Be(totalSize);
        writeReports[^1].Percentage.Should().Be(100.0);
    }

    [Fact]
    public void StreamProgress_EqualityAndZeroLengthPercentage()
    {
        var a = new StreamProgress(10, 50, 100);
        var b = new StreamProgress(10, 50, 100);
        var c = new StreamProgress(10, 60, 100);
        var zeroLength = new StreamProgress(0, 0, 0);

        a.BytesTransferred.Should().Be(10L);
        a.TotalBytesTransferred.Should().Be(50L);
        a.TotalLength.Should().Be(100L);
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
        a.Equals((object)b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
        (a != c).Should().BeTrue();
        a.Equals(null).Should().BeFalse();
        zeroLength.Percentage.Should().BeNull();
    }
}
