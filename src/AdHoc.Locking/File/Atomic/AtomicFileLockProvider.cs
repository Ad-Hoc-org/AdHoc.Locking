// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;

namespace AdHoc.Locking;
public sealed class AtomicFileLockProvider
    : IDistributedAtomicLockProvider
{


    private const int _DefaultTimeToLiveKey = 0;


    public string LocksDirectory { get; }


    private readonly ConcurrentDictionary<string, AtomicFileLock> _atomics;
    private readonly Dictionary<object, TimeSpan> _timeToLives;


    public AtomicFileLockProvider(string locksDirectory)
    {
        LocksDirectory = Path.GetFullPath(locksDirectory);
        _atomics = new();
        _timeToLives = [];
    }


    public IDistributedAtomicLock GetLock(string name) =>
        _atomics.GetOrAdd(name, name =>
        {
            lock (_timeToLives)
            {
                if (!_timeToLives.TryGetValue(name, out TimeSpan timeToLive))
                    if (!_timeToLives.TryGetValue(_DefaultTimeToLiveKey, out timeToLive))
                        timeToLive = LockFiles._DefaultTimeToLive;
                return new(name, timeToLive);
            }
        });


    public void SetTimeToLive(string? name, TimeSpan timeToLive)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeToLive, TimeSpan.Zero);
        lock (_timeToLives)
        {
            _timeToLives[name is null ? _DefaultTimeToLiveKey : name] = timeToLive;
            if (name is not null && _atomics.TryGetValue(name, out AtomicFileLock? atomic))
                atomic.TimeToLive = timeToLive;
        }
    }


}
