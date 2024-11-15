using System.ComponentModel;

namespace AdHoc.Locking;

[EditorBrowsable(EditorBrowsableState.Advanced)]
public class AcquiringValueTaskSource<TLocking>
    where TLocking : ILocking
{

    public TLocking Locking { get; }

    private readonly Lock _stateLock;
    private readonly Action<AcquiringValueTaskSource<TLocking>> _cancel;

    private readonly HashSet<ValueTaskSource> _sources;


#if NET9_0_OR_GREATER
    public
#else
    internal
#endif
    AcquiringValueTaskSource(TLocking @lock, Lock stateLock, Action<AcquiringValueTaskSource<TLocking>> cancel)
    {
        Locking = @lock;
        _stateLock = stateLock;
        _cancel = cancel;
        _sources = [];
    }

#if !NET9_0_OR_GREATER
    public AcquiringValueTaskSource(TLocking @lock, object stateLock, Action<AcquiringValueTaskSource<TLocking>> cancel)
        : this(@lock, new Lock(stateLock), cancel) { }
#endif


    public void Acquired()
    {
        using (_stateLock.EnterScope())
        {
            foreach (ValueTaskSource source in _sources)
                source.Acquired();
            _sources.Clear();
        }
    }


    public ValueTask AcquiringAsync(CancellationToken cancellationToken)
    {
        using (_stateLock.EnterScope())
        {
            ValueTaskSource source = new(this, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                source.Dispose();
                return ValueTask.FromCanceled(cancellationToken);
            }

            _sources.Add(source);
            return source.Task;
        }
    }


    public void Throw(Exception exception)
    {
        using (_stateLock.EnterScope())
        {
            foreach (ValueTaskSource source in _sources)
                source.Throw(exception);
            _sources.Clear();
        }
    }


    private void Remove(ValueTaskSource source)
    {
        using (_stateLock.EnterScope())
            if (_sources.Remove(source) && _sources.Count == 0)
                _cancel(this);
    }


    private sealed class ValueTaskSource
        : IDisposable
    {


        private readonly TaskCompletionSource _core;
        public ValueTask Task => new(_core.Task);


        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenRegistration _cancellation;

        private readonly AcquiringValueTaskSource<TLocking> _source;


        internal ValueTaskSource(AcquiringValueTaskSource<TLocking> source, CancellationToken cancellationToken)
        {
            _core = new();
            _cancellationToken = cancellationToken;
            _cancellation = cancellationToken.Register(Cancel);
            _source = source;
        }


        internal void Acquired()
        {
            _core.SetResult();
            _cancellation.Dispose();
        }

        internal void Throw(Exception exception)
        {
            _core.SetException(exception);
            _cancellation.Dispose();
        }


        private void Cancel()
        {
            _core.SetCanceled(_cancellationToken);
            _cancellation.Dispose();
            _source.Remove(this);
        }


        public void Dispose() => _cancellation.Dispose();

    }
}
