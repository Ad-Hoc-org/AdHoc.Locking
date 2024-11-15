// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

#if !NET9_0_OR_GREATER
#pragma warning disable CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
using System.ComponentModel;

namespace System.Threading;
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class Lock
{


    private readonly object _lock;

    public Lock() =>
        _lock = this;

    public Lock(object @lock) =>
        _lock = @lock;

    public Scope EnterScope()
    {
        Monitor.Enter(_lock);
        return new Scope(this);
    }

    public ref struct Scope(Lock @lock)
        : IDisposable
    {
        public readonly void Dispose() =>
            Monitor.Exit(@lock._lock);
    }
}
#pragma warning restore CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
#endif
