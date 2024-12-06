namespace AdHoc.Locking.Abstractions;
public static partial class Locks
{


    public static ISemaphoreLocking Acquire(
        this ISemaphore factory,
        int requiredCount
    )
    {
        ISemaphoreLocking semaphore = factory.Create();
        semaphore.Acquire(requiredCount);
        return semaphore;
    }

    public static async ValueTask<ISemaphoreLocking> AcquireAsync(
        this ISemaphore factory,
        int requiredCount,
        CancellationToken cancellationToken
    )
    {
        ISemaphoreLocking semaphore = factory.Create();
        await semaphore.AcquireAsync(requiredCount, cancellationToken);
        return semaphore;
    }


}
