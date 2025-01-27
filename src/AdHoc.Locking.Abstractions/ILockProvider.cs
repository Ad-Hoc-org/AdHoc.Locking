namespace AdHoc.Locking.Abstractions;
public interface ILockProvider
{
    ILock GetLock(string name);
}
