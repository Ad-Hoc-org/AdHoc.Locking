using System.Collections.Concurrent;

namespace AdHoc.Locking;
public sealed class SemaphoreProvider
    : ISemaphoreProvider
{


    private const int _DefaultCountKey = 0;


    private readonly Dictionary<object, int> _counts;

    private readonly ConcurrentDictionary<string, Semaphore> _semaphores;


    public SemaphoreProvider()
    {
        _counts = [];
        _semaphores = new();
    }


    public ISemaphore GetLock(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _semaphores.GetOrAdd(name, name =>
        {
            lock (_counts)
            {
                if (!_counts.TryGetValue(name, out int count))
                    if (!_counts.TryGetValue(_DefaultCountKey, out count))
                        count = 1;
                return new(name, count);
            }
        });
    }


    public void SetSemaphore(string? name, int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        lock (_counts)
        {
            _counts[name is null ? _DefaultCountKey : name] = count;
            if (name is not null && _semaphores.TryGetValue(name, out Semaphore? semaphore))
                semaphore.SemaphoreCount = count;
        }
    }

}
