namespace AdHoc.Locking.Abstraction;
public static partial class Locks
{


    public static ILocking Acquire(
        this ILock factory
    )
    {
        ILocking @lock = factory.Create();
        @lock.Acquire();
        return @lock;
    }


    public static TLock Acquire<TLock>(
        this ILock<TLock> factory
    )
        where TLock : ILocking
    {
        TLock @lock = factory.Create();
        @lock.Acquire();
        return @lock;
    }


    public static async ValueTask<ILocking> AcquireAsync(
        this ILock factory,
        CancellationToken cancellationToken
    )
    {
        ILocking @lock = factory.Create();
        await @lock.AcquireAsync(cancellationToken);
        return @lock;
    }


    public static async ValueTask<TLock> AcquireAsync<TLock>(
        this ILock<TLock> factory,
        CancellationToken cancellationToken
    )
        where TLock : ILocking
    {
        TLock @lock = factory.Create();
        await @lock.AcquireAsync(cancellationToken);
        return @lock;
    }


}
