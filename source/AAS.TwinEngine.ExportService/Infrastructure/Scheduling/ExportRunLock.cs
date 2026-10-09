namespace AAS.TwinEngine.ExportService.Infrastructure.Scheduling;

public sealed class ExportRunLock : IExportRunLock
{
    private int _busy;

    public IDisposable? TryAcquire()
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
        {
            return null;
        }

        return new Releaser(this);
    }

    private void Release() => Interlocked.Exchange(ref _busy, 0);

    private sealed class Releaser : IDisposable
    {
        private readonly ExportRunLock _owner;
        private int _disposed;

        public Releaser(ExportRunLock owner) => _owner = owner;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _owner.Release();
            }
        }
    }
}
