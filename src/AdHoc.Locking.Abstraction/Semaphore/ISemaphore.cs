namespace AdHoc.Locking.Abstraction;
public interface ISemaphore
    : ILock<ISemaphoreLocking>
{
    int SemaphoreCount { get; }

    int AcquiredCount { get; }
}
