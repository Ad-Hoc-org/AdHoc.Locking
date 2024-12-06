using System.Diagnostics.CodeAnalysis;

namespace AdHoc.Locking;

public sealed partial class Semaphore
{
    private sealed class Locking
        : ISemaphoreLocking
    {


        public string LockName =>
            _semaphore.Name;


        public int SemaphoreCount =>
            _semaphore.SemaphoreCount;

        public int TotalAcquiredCount =>
            _semaphore.AcquiredCount;


        public int AcquiredCount { get; private set; }


        private readonly Semaphore _semaphore;


        private readonly Lock _lock = new();

        private readonly SortedList<int, AcquiringValueTaskSource<Locking>> _acquiring;


        internal Locking(Semaphore semaphore)
        {
            _semaphore = semaphore;
            _acquiring = [];
        }


        public bool TryAcquire(int requiredCount, CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(requiredCount, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(requiredCount, _semaphore.SemaphoreCount);

            if (AcquiredCount >= requiredCount)
                return true;

            using (_lock.EnterScope())
            {
                if (AcquiredCount >= requiredCount)
                    return true;

                cancellationToken.ThrowIfCancellationRequested();

                int neededCount = requiredCount - AcquiredCount;
                lock (_semaphore._acquired)
                {
                    if (_semaphore.AcquiredCount + neededCount > _semaphore.SemaphoreCount)
                        return false;

                    _semaphore._acquired[this] = requiredCount;
                    _semaphore.AcquiredCount += neededCount;
                    AcquiredCount = requiredCount;
                    return true;
                }
            }
        }

        public ValueTask<bool> TryAcquireAsync(int requiredCount, CancellationToken cancellationToken) =>
            ValueTask.FromResult(TryAcquire(requiredCount, cancellationToken));



        [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Should explicit block current thread.")]
        [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Should explicit block current thread.")]
        public void Acquire(int requiredCount, CancellationToken cancellationToken = default) =>
            AcquireAsync(requiredCount, cancellationToken).GetAwaiter().GetResult();

        public ValueTask AcquireAsync(int requiredCount, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(requiredCount, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(requiredCount, _semaphore.SemaphoreCount);

            if (AcquiredCount >= requiredCount)
                return ValueTask.CompletedTask;

            using (_lock.EnterScope())
            {
                if (AcquiredCount >= requiredCount)
                    return ValueTask.CompletedTask;

                if (_acquiring.Count > 0)
                {
                    int acquiringCount = _acquiring.GetKeyAtIndex(_acquiring.Count - 1);
                    if (acquiringCount >= requiredCount)
                    {
                        int index = _acquiring.IndexOfKey(requiredCount);
                        AcquiringValueTaskSource<Locking> source;
                        if (index < 0)
                        {
                            source = new(this, _lock, CancelAcquiring);
                            _acquiring.Add(requiredCount, source);
                        }
                        else
                        {
                            source = _acquiring.GetValueAtIndex(index);
                        }

                        return source.AcquiringAsync(cancellationToken);
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled(cancellationToken);

                int neededCount = requiredCount - AcquiredCount;
                lock (_semaphore._acquired)
                {
                    if (_semaphore.AcquiredCount + neededCount <= _semaphore.SemaphoreCount)
                    {
                        _semaphore.AcquiredCount += neededCount;
                        _semaphore._acquired[this] = requiredCount;
                        AcquiredCount = requiredCount;
                        return ValueTask.CompletedTask;
                    }

                    _semaphore._queue.AddLast(new Acquisition(requiredCount, this));
                    AcquiringValueTaskSource<Locking> source = new(this, _lock, CancelAcquiring);
                    _acquiring.Add(requiredCount, source);
                    return source.AcquiringAsync(cancellationToken);
                }
            }
        }


        private static void CancelAcquiring(AcquiringValueTaskSource<Locking> source)
        {
            Locking semaphore = source.Locking;
            SortedList<int, AcquiringValueTaskSource<Locking>> acquiring = semaphore._acquiring;
            var queue = semaphore._semaphore._queue;
            lock (queue)
            {
                int index = acquiring.IndexOfValue(source);
                if (index >= 0)
                {
                    int count = acquiring.GetKeyAtIndex(index);
                    acquiring.RemoveAt(index);
                    semaphore.RemoveAcquistion(c => c == count);
                }
            }
        }


        private void RemoveAcquistion(Predicate<int> removePredicate)
        {
            LinkedListNode<Acquisition>? node = _semaphore._queue.First;
            while (node is not null)
            {
                var acquistion = node.ValueRef;
                if (ReferenceEquals(acquistion.Semaphore, this) && removePredicate(acquistion.Count))
                    _semaphore._queue.Remove(node);
                node = node.Next;
            }
        }


        public void Release(int remainingCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(remainingCount);

            if (AcquiredCount <= remainingCount && _acquiring.Count == 0)
                return;

            using (_lock.EnterScope())
            {
                bool wasAcquiring = false;
                for (int i = _acquiring.Count; i-- > 0;)
                {
                    if (_acquiring.GetKeyAtIndex(i) > remainingCount)
                    {
                        _acquiring.GetValueAtIndex(i).Throw(new SynchronizationLockException());
                        _acquiring.RemoveAt(i);
                        wasAcquiring = true;
                    }
                    else
                        break;
                }

                if (AcquiredCount > remainingCount)
                {
                    while (true)
                    {
                        Acquisition? next = _semaphore._queue.First?.ValueRef;
                        if (next is null)
                        {
                            lock (_semaphore._queue)
                            {
                                if (_semaphore._queue.First is not null)
                                    continue; // next changed while locking

                                if (wasAcquiring)
                                    RemoveAcquistion(c => c > remainingCount);

                                Releasing(remainingCount, out _);
                                break;
                            }
                        }
                        else
                        {
                            using (next.Semaphore._lock.EnterScope())
                            {
                                lock (_semaphore._queue)
                                {
                                    if (!ReferenceEquals(_semaphore._queue.First?.ValueRef, next))
                                        continue; // next changed while locking

                                    if (wasAcquiring)
                                        RemoveAcquistion(c => c > remainingCount);

                                    Releasing(remainingCount, next);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }


        private void Releasing(int remainingCount, out int releasedCount)
        {
            if (remainingCount == 0)
            {
                _semaphore.AcquiredCount -= AcquiredCount;
                _semaphore._acquired.Remove(this);
                AcquiredCount = 0;
            }
            else
            {
                _semaphore.AcquiredCount -= AcquiredCount - remainingCount;
                _semaphore._acquired[this] = AcquiredCount = remainingCount;
            }

            releasedCount = _semaphore.SemaphoreCount - _semaphore.AcquiredCount;
        }

        private void Releasing(int remainingCount, Acquisition next)
        {
            Releasing(remainingCount, out int releasedCount);
            while (releasedCount > 0 && next is not null)
            {
                Locking semaphore = next.Semaphore;
                int acquiringCount = Math.Min(releasedCount, next.Count - semaphore.AcquiredCount);

                _semaphore.AcquiredCount += acquiringCount;
                int acquiredCount = _semaphore._acquired[semaphore] = semaphore.AcquiredCount += acquiringCount;

                if (acquiredCount == next.Count)
                {
                    _semaphore._queue.Remove(next);
                    next = _semaphore._queue.First?.ValueRef!;
                }

                while (semaphore._acquiring.Count > 0)
                {
                    int count = semaphore._acquiring.GetKeyAtIndex(0);
                    if (count > acquiredCount)
                        break;
                    AcquiringValueTaskSource<Locking> source = semaphore._acquiring.GetValueAtIndex(0);
                    semaphore._acquiring.RemoveAt(0);
                    source.Acquired();
                }

                releasedCount -= acquiringCount;
            }
        }


        public ValueTask ReleaseAsync(int remainingCount)
        {
            Release(remainingCount);
            return ValueTask.CompletedTask;
        }


    }
}
