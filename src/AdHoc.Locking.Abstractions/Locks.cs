namespace AdHoc.Locking.Abstractions;
public static partial class Locks
{


    public static ILocking Acquire(
        this ILock @lock
    )
    {
        ILocking locking = @lock.Create();
        locking.Acquire();
        return locking;
    }


    public static TLocking Acquire<TLocking>(
        this ILock<TLocking> @lock
    )
        where TLocking : ILocking
    {
        TLocking locking = @lock.Create();
        locking.Acquire();
        return locking;
    }


    public static async ValueTask<ILocking> AcquireAsync(
        this ILock @lock,
        CancellationToken cancellationToken
    )
    {
        ILocking locking = @lock.Create();
        await locking.AcquireAsync(cancellationToken);
        return locking;
    }


    public static async ValueTask<TLocking> AcquireAsync<TLocking>(
        this ILock<TLocking> @lock,
        CancellationToken cancellationToken
    )
        where TLocking : ILocking
    {
        TLocking locking = @lock.Create();
        await locking.AcquireAsync(cancellationToken);
        return locking;
    }


}
