namespace AdHoc.Locking.Abstraction;

public interface ILocking
    : IDisposable,
        IAsyncDisposable
{


    string LockName { get; }


    bool TryAcquire(CancellationToken cancellationToken = default);

    ValueTask<bool> TryAcquireAsync(CancellationToken cancellationToken);


    void Acquire(CancellationToken cancellationToken = default);

    ValueTask AcquireAsync(CancellationToken cancellationToken);


    void Release();

    ValueTask ReleaseAsync();


    void IDisposable.Dispose()
    {
        Release();
        GC.SuppressFinalize(this);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await ReleaseAsync();
        GC.SuppressFinalize(this);
    }
}
