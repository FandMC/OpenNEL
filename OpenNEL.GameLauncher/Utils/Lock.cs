namespace OpenNEL.GameLauncher.Utils;

public class Lock
{
    private sealed class Scope : IDisposable
    {
        private readonly Lock _owner;
        private bool _disposed;

        public Scope(Lock owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Monitor.Exit(_owner._sync);
            }
        }
    }

    private readonly object _sync = new();

    public IDisposable EnterScope()
    {
        Monitor.Enter(_sync);
        return new Scope(this);
    }
}
