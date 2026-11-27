namespace ProgressStream.Tests;

public class ProgressStreamPropertyTests
{
    [Fact]
    public async Task Properties()
    {
        Stream inputStream = new MemoryStream(new byte[10]);

        await using var stream = new ProgressStream(inputStream);

        stream.CanRead.Should().Be(inputStream.CanRead);
        stream.CanSeek.Should().Be(inputStream.CanSeek);
        stream.CanWrite.Should().Be(inputStream.CanWrite);
        stream.Length.Should().Be(inputStream.Length);

        inputStream.Position = 1;

        stream.Position.Should().Be(inputStream.Position);

        stream.Position = 2;

        stream.Position.Should().Be(inputStream.Position);
    }

    [Fact]
    public async Task Seek()
    {
        Stream inputStream = new MemoryStream(new byte[10]);

        await using var stream = new ProgressStream(inputStream);

        stream.Seek(5, SeekOrigin.Begin);

        inputStream.Position.Should().Be(5);
        stream.Position.Should().Be(5);
    }

    [Fact]
    public async Task SetLength()
    {
        Stream inputStream = new MemoryStream(new byte[10]);

        await using var stream = new ProgressStream(inputStream);

        stream.SetLength(5);

        inputStream.Length.Should().Be(5);
        stream.Length.Should().Be(5);
    }
}
