namespace AdHoc.Locking.Abstractions;
public interface ISemaphoreProvider
    : ILockProvider<ISemaphore>
{
    void SetSemaphoreCount(string? name, int count);
}
