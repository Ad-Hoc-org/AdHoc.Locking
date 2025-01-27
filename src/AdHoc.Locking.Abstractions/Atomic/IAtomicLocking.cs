namespace AdHoc.Locking.Abstractions;
public interface IAtomicLocking
    : ILocking
{
    bool IsAcquired { get; }

    ValueTask<bool> IsAcquiredAsync(CancellationToken cancellationToken);
}
