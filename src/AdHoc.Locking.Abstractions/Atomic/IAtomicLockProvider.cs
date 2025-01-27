namespace AdHoc.Locking.Abstractions;
public interface IAtomicLockProvider
    : ILockProvider
{
    IAtomicLock GetAtomic(string name);

    ILock ILockProvider.GetLock(string name) =>
        GetAtomic(name);
}
