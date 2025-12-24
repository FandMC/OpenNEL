namespace OpenNEL.GameLauncher.Utils.Progress;

public class SyncCallback<T> : IProgress<T>
{
    private readonly Action<T> _handler;

    public SyncCallback(Action<T> handler)
    {
        _handler = handler;
    }

    public void Report(T value)
    {
        _handler(value);
    }
}
