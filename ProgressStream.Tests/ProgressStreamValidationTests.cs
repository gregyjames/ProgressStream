namespace ProgressStream.Tests;

public class ProgressStreamValidationTests
{
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
}
