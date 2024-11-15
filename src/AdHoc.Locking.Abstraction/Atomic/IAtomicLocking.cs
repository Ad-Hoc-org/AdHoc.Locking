namespace AdHoc.Locking.Abstraction;
public interface IAtomicLocking
    : ILocking
{
    bool IsAcquired { get; }
}
