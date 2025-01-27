// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;

namespace AdHoc.Locking;
public sealed class AtomicFileLockProvider
    : IAtomicLockProvider
{

    public string LocksDirectory { get; }

    private readonly ConcurrentDictionary<string, AtomicFileLock> _atomics;
    private readonly Func<string, TimeSpan> _timeToLive;

    public AtomicFileLockProvider(string locksDirectory, Func<string, TimeSpan> timeToLive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locksDirectory);
        ArgumentNullException.ThrowIfNull(timeToLive);
        LocksDirectory = Path.GetFullPath(locksDirectory);
        _timeToLive = timeToLive;
        _atomics = new();
    }

    public IAtomicLock GetAtomic(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _atomics.GetOrAdd(name, name => new(Path.Combine(LocksDirectory, name), name, _timeToLive));
    }
}
