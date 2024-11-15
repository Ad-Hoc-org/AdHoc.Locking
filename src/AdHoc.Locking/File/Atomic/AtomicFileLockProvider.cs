// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;

namespace AdHoc.Locking;
public sealed class AtomicFileLockProvider
    : IDistributedAtomicLockProvider
{


    private const int _DefaultExpiryIntervalKey = 0;


    public string LocksDirectory { get; }


    private readonly ConcurrentDictionary<string, AtomicFileLock> _atomics;
    private readonly ConcurrentDictionary<object, TimeSpan> _expiryIntervals;


    public AtomicFileLockProvider(string locksDirectory)
    {
        LocksDirectory = Path.GetFullPath(locksDirectory);
        _atomics = new();
        _expiryIntervals = new();
    }


    public IDistributedAtomicLock GetLock(string name) =>
        _atomics.GetOrAdd(name, name =>
        {
            lock (_expiryIntervals)
            {
                if (!_expiryIntervals.TryGetValue(name, out TimeSpan expiryInterval))
                    if (!_expiryIntervals.TryGetValue(_DefaultExpiryIntervalKey, out expiryInterval))
                        expiryInterval = TimeSpan.FromMinutes(1);
                return new(name, expiryInterval);
            }
        });


    public void SetExpiryInterval(string? name, TimeSpan expiryInterval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiryInterval, TimeSpan.Zero);
        lock (_expiryIntervals)
        {
            _expiryIntervals[name is null ? _DefaultExpiryIntervalKey : name] = expiryInterval;
            if (name is not null && _atomics.TryGetValue(name, out AtomicFileLock? atomic))
                atomic.ExpiryInterval = expiryInterval;
        }
    }


}
