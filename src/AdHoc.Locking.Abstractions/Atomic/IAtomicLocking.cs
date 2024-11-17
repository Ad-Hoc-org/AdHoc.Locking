namespace AdHoc.Locking.Abstractions;
public interface IAtomicLocking
    : ILocking
{
    bool IsAcquired { get; }
}
