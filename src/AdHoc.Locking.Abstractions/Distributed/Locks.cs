namespace AdHoc.Locking.Abstractions;
public static partial class Locks
{

    public static bool TryAcquire(
        this ILocking locking,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default
    ) =>
        locking is IDistributedLocking distributed
            ? distributed.TryAcquire(expiresIn, cancellationToken)
            : locking.TryAcquire(cancellationToken);

    public static ValueTask<bool> TryAcquireAsync(
        this ILocking locking,
        TimeSpan expiresIn,
        CancellationToken cancellationToken
    ) =>
        locking is IDistributedLocking distributed
            ? distributed.TryAcquireAsync(expiresIn, cancellationToken)
            : locking.TryAcquireAsync(cancellationToken);

    public static void Acquire(
        this ILocking locking,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default
    )
    {
        if (locking is IDistributedLocking distributed)
            distributed.Acquire(expiresIn, cancellationToken);
        else
            locking.Acquire(cancellationToken);
    }

    public static ValueTask AcquireAsync(
        this ILocking locking,
        TimeSpan expiresIn,
        CancellationToken cancellationToken
    ) =>
        locking is IDistributedLocking distributed
            ? distributed.AcquireAsync(expiresIn, cancellationToken)
            : locking.AcquireAsync(cancellationToken);


    public static ILocking Acquire(
        this ILock @lock,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default
    )
    {
        ILocking locking = @lock.Create();
        locking.Acquire(expiresIn, cancellationToken);
        return locking;
    }

    public static async ValueTask<ILocking> AcquireAsync(
        this ILock @lock,
        TimeSpan expiresIn,
        CancellationToken cancellationToken
    )
    {
        ILocking locking = @lock.Create();
        await locking.AcquireAsync(expiresIn, cancellationToken);
        return locking;
    }

}
