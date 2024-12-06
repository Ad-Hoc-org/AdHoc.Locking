// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT


using System.Collections.Concurrent;

namespace AdHoc.Locking;
public sealed class FileSemaphoreProvider
    : IDistributedSemaphoreProvider
{


    private const int _DefaultTimeToLiveKey = 0;
    private const int _DefaultCountKey = 0;


    public string LocksDirectory { get; }


    private readonly Dictionary<object, int> _counts;
    private readonly Dictionary<object, TimeSpan> _timeToLives;

    private readonly ConcurrentDictionary<string, FileSemaphore> _semaphores;


    public FileSemaphoreProvider(string locksDirectory)
    {
        LocksDirectory = Path.GetFullPath(locksDirectory);
        _semaphores = new();
        _counts = [];
        _timeToLives = [];
    }


    public IDistributedSemaphore GetLock(string name) =>
        _semaphores.GetOrAdd(name, name =>
        {
            int count;
            lock (_counts)
            {
                if (!_counts.TryGetValue(name, out count))
                    if (!_counts.TryGetValue(_DefaultCountKey, out count))
                        count = 1;
            }
            TimeSpan timeToLive;
            lock (_timeToLives)
            {
                if (!_timeToLives.TryGetValue(name, out timeToLive))
                    if (!_timeToLives.TryGetValue(_DefaultTimeToLiveKey, out timeToLive))
                        timeToLive = LockFiles._DefaultTimeToLive;
            }
            return new(name, count, timeToLive);
        });


    public void SetTimeToLive(string? name, TimeSpan timeToLive)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeToLive, TimeSpan.Zero);
        lock (_timeToLives)
        {
            _timeToLives[name is null ? _DefaultTimeToLiveKey : name] = timeToLive;
            if (name is not null && _semaphores.TryGetValue(name, out FileSemaphore? semaphore))
                semaphore.TimeToLive = timeToLive;
        }
    }


    public void SetSemaphoreCount(string? name, int count) => throw new NotImplementedException();
    public ValueTask SetSemaphoreCountAsync(string? name, int count, CancellationToken cancellationToken) => throw new NotImplementedException();
}
