namespace ProgressStream.Tests;

public class ProgressStreamValidationTests
{
    [Fact]
    public void Constructor_InvalidArguments_ThrowExpectedExceptions()
    {
        FluentActions.Invoking(() => new ProgressStream(null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("stream");

        FluentActions.Invoking(() => new ProgressStream(null!, (IProgress<int>?)null))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("stream");

        FluentActions.Invoking(() => new ProgressStream(null!, (IProgress<StreamProgress>?)null))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("stream");

        using var inner = new MemoryStream();
        FluentActions.Invoking(() => new ProgressStream(inner, totalLength: -1))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("totalLength");
    }

    [Fact]
    public void ReadAndWrite_InvalidBufferArguments_ThrowExpectedExceptions()
    {
        using var inner = new MemoryStream(new byte[10]);
        using var stream = new ProgressStream(inner);
        var buffer = new byte[10];

        FluentActions.Invoking(() => stream.Read(null!, 0, 1))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("buffer");
        FluentActions.Invoking(() => stream.Read(buffer, -1, 1))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("offset");
        FluentActions.Invoking(() => stream.Read(buffer, 0, -1))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("count");
        FluentActions.Invoking(() => stream.Read(buffer, 5, 6))
            .Should().Throw<ArgumentException>()
            .WithParameterName("buffer");

        FluentActions.Invoking(() => stream.Write(null!, 0, 1))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("buffer");
        FluentActions.Invoking(() => stream.Write(buffer, -1, 1))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("offset");
        FluentActions.Invoking(() => stream.Write(buffer, 0, -1))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("count");
        FluentActions.Invoking(() => stream.Write(buffer, 5, 6))
            .Should().Throw<ArgumentException>()
            .WithParameterName("buffer");
    }
}
