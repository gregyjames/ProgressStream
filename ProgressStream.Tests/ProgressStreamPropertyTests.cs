namespace ProgressStream.Tests;

public class ProgressStreamPropertyTests
{
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
}
