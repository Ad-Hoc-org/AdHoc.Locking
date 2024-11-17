namespace AdHoc.Locking.Abstractions;
public interface ISemaphoreProvider
    : ILockProvider<ISemaphore>
{
    void SetSemaphore(string? name, int count);
}
