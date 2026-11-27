namespace ProgressStream.Tests.TestDoubles;

internal sealed class UnitTestProgress<T>(Action<T> handler) : IProgress<T>
{
    private readonly Action<T> _handler = handler;

    public void Report(T value)
    {
        _handler(value);
    }
}
