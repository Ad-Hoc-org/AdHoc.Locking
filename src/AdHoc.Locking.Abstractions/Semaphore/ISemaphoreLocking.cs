namespace AdHoc.Locking.Abstractions;
public interface ISemaphoreLocking
    : ILocking
{


    int SemaphoreCount { get; }

    int TotalAcquiredCount { get; }

    int AcquiredCount { get; }


    bool TryAcquire(int requiredCount, CancellationToken cancellationToken = default);

    bool ILocking.TryAcquire(CancellationToken cancellationToken) =>
        TryAcquire(1, cancellationToken);

    ValueTask<bool> TryAcquireAsync(int requiredCount, CancellationToken cancellationToken);

    ValueTask<bool> ILocking.TryAcquireAsync(CancellationToken cancellationToken) =>
        TryAcquireAsync(1, cancellationToken);


    void Acquire(int requiredCount, CancellationToken cancellationToken = default);

    void ILocking.Acquire(CancellationToken cancellationToken) =>
        Acquire(1, cancellationToken);

    ValueTask AcquireAsync(int requiredCount, CancellationToken cancellationToken);

    ValueTask ILocking.AcquireAsync(CancellationToken cancellationToken) =>
        AcquireAsync(1, cancellationToken);


    void Release(int remainingCount);

    void ILocking.Release() =>
        Release(0);

    ValueTask ReleaseAsync(int remainingCount);

    ValueTask ILocking.ReleaseAsync() =>
        ReleaseAsync(0);


}
