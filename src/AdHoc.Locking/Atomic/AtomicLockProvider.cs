using System.Collections.Concurrent;

namespace AdHoc.Locking;
public sealed class AtomicLockProvider
    : IAtomicLockProvider
{

    private readonly ConcurrentDictionary<string, AtomicLock> _atomics;

    public AtomicLockProvider() =>
        _atomics = new();

    public IAtomicLock GetAtomic(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _atomics.GetOrAdd(name, static name => new(name));
    }

}
