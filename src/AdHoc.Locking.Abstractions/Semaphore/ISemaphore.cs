namespace AdHoc.Locking.Abstractions;
public interface ISemaphore
    : ILock<ISemaphoreLocking>
{
    int SemaphoreCount { get; set; }

    int AcquiredCount { get; }
}
