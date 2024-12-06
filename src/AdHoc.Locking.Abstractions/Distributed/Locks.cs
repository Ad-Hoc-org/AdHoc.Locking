namespace AdHoc.Locking.Abstractions;
public static partial class Locks
{


    public static IDistributedLocking Acquire(
        this IDistributedLock @lock,
        string owner
    )
    {
        IDistributedLocking locking = @lock.Create(owner);
        locking.Acquire();
        return locking;
    }

    public static IDistributedLocking Acquire(
        this IDistributedLock @lock,
        string owner,
        TimeSpan expiresIn
    )
    {
        IDistributedLocking locking = @lock.Create(owner);
        locking.Acquire(expiresIn);
        return locking;
    }


    public static TLocking Acquire<TLocking>(
        this IDistributedLock<TLocking> @lock,
        string owner
    )
        where TLocking : IDistributedLocking
    {
        TLocking locking = @lock.Create(owner);
        locking.Acquire();
        return locking;
    }

    public static TLocking Acquire<TLocking>(
        this IDistributedLock<TLocking> @lock,
        string owner,
        TimeSpan expiresIn
    )
        where TLocking : IDistributedLocking
    {
        TLocking locking = @lock.Create(owner);
        locking.Acquire(expiresIn);
        return locking;
    }


    public static async ValueTask<IDistributedLocking> AcquireAsync(
        this IDistributedLock @lock,
        string owner,
        CancellationToken cancellationToken
    )
    {
        IDistributedLocking locking = @lock.Create(owner);
        await locking.AcquireAsync(cancellationToken);
        return locking;
    }

    public static async ValueTask<IDistributedLocking> AcquireAsync(
        this IDistributedLock @lock,
        string owner,
        TimeSpan expiresIn,
        CancellationToken cancellationToken
    )
    {
        IDistributedLocking locking = @lock.Create(owner);
        await locking.AcquireAsync(expiresIn, cancellationToken);
        return locking;
    }


    public static async ValueTask<TLocking> AcquireAsync<TLocking>(
        this IDistributedLock<TLocking> @lock,
        string owner,
        CancellationToken cancellationToken
    )
        where TLocking : IDistributedLocking
    {
        TLocking locking = @lock.Create(owner);
        await locking.AcquireAsync(cancellationToken);
        return locking;
    }

    public static async ValueTask<TLocking> AcquireAsync<TLocking>(
        this IDistributedLock<TLocking> @lock,
        string owner,
        TimeSpan expiresIn,
        CancellationToken cancellationToken
    )
        where TLocking : IDistributedLocking
    {
        TLocking locking = @lock.Create(owner);
        await locking.AcquireAsync(expiresIn, cancellationToken);
        return locking;
    }


}
