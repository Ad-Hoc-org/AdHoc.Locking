using System.Diagnostics.CodeAnalysis;

namespace AdHoc.Locking;
public sealed class AtomicLock
    : IAtomicLock
{


    public string Name { get; }


    private Locking? _current;

    private readonly LinkedList<Locking> _queue = new();


    public AtomicLock(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }


    public IAtomicLocking Create() =>
        new Locking(this);


    private sealed class Locking
        : IAtomicLocking
    {


        public string LockName =>
            _atomic.Name;


        public bool IsAcquired { get; private set; }


        private readonly AtomicLock _atomic;

        private readonly Lock _lock = new();

        private AcquiringValueTaskSource<Locking>? _acquiring;


        internal Locking(AtomicLock atomic) =>
            _atomic = atomic;


        public bool TryAcquire(CancellationToken cancellationToken = default)
        {
            if (IsAcquired)
                return true;

            using (_lock.EnterScope())
            {
                if (IsAcquired)
                    return true;

                cancellationToken.ThrowIfCancellationRequested();

                lock (_atomic._queue)
                {
                    if (_atomic._current is not null)
                        return false;

                    _atomic._current = this;
                    IsAcquired = true;
                    return true;
                }
            }
        }

        public ValueTask<bool> TryAcquireAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult(TryAcquire(cancellationToken));



        [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Should explicit block current thread.")]
        [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Should explicit block current thread.")]
        public void Acquire(CancellationToken cancellationToken = default) =>
            AcquireAsync(cancellationToken).GetAwaiter().GetResult();

        public ValueTask AcquireAsync(CancellationToken cancellationToken)
        {
            if (IsAcquired)
                return ValueTask.CompletedTask;

            using (_lock.EnterScope())
            {
                if (IsAcquired)
                    return ValueTask.CompletedTask;

                if (_acquiring is not null)
                    return _acquiring.AcquiringAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return ValueTask.FromCanceled(cancellationToken);

                lock (_atomic._queue)
                {
                    if (_atomic._current is null)
                    {
                        _atomic._current = this;
                        IsAcquired = true;
                        return ValueTask.CompletedTask;
                    }

                    _acquiring = new(this, _lock, static source => source.Locking.Release());
                    _atomic._queue.AddLast(this);
                    return _acquiring.AcquiringAsync(cancellationToken);
                }
            }
        }


        public void Release()
        {
            if (!IsAcquired && _acquiring is null)
                return;

            using (_lock.EnterScope())
            {
                if (!IsAcquired && _acquiring is null)
                    return;

                if (_acquiring is not null)
                {
                    _acquiring.Throw(new SynchronizationLockException());
                    _acquiring = null;
                }

                while (true)
                {
                    Locking? next = _atomic._queue.First?.ValueRef;
                    if (next is null)
                    {
                        lock (_atomic._queue)
                        {
                            if (_atomic._queue.First is not null)
                                continue; // next changed while locking

                            Releasing();
                            break;
                        }
                    }
                    else
                    {
                        using (next._lock.EnterScope())
                        {
                            lock (_atomic._queue)
                            {
                                if (!ReferenceEquals(_atomic._queue.First?.ValueRef, next))
                                    continue; // next changed while locking

                                Releasing(next);
                                break;
                            }
                        }
                    }
                }
            }
        }


        private void Releasing()
        {
            IsAcquired = false;

            if (ReferenceEquals(_atomic._current, this))
                _atomic._current = null;
            else
                _atomic._queue.Remove(this);
        }

        private void Releasing(Locking next)
        {
            IsAcquired = false;

            if (ReferenceEquals(_atomic._current, this))
            {
                _atomic._current = next;
                _atomic._queue.RemoveFirst();
                next.IsAcquired = true;
                next._acquiring!.Acquired();
                next._acquiring = null;
            }
            else
                _atomic._queue.Remove(this);
        }


        public ValueTask ReleaseAsync()
        {
            Release();
            return ValueTask.CompletedTask;
        }


    }
}
