namespace AdHoc.Locking.Abstraction;
public interface ISemaphoreProvider
    : ILockProvider<ISemaphore>
{
    void SetSemaphore(string? name, int count);
}
