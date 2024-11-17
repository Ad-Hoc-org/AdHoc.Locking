namespace AdHoc.Locking.Abstractions;
public interface ISemaphore
    : ILock<ISemaphoreLocking>
{
    int SemaphoreCount { get; }

    int AcquiredCount { get; }
}
