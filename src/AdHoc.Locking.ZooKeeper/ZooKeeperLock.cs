// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using AdHoc.Locking.Abstractions;
using org.apache.zookeeper;
using Keeper = org.apache.zookeeper.ZooKeeper;

namespace AdHoc.Locking.ZooKeeper;

/// <summary>
/// Atomic look based on https://zookeeper.apache.org/doc/r3.1.2/recipes.html
/// </summary>
public sealed class AtomicLock
    : IAtomicLock,
        IDistributedLock
{


    private readonly Keeper _keeper = null!;



    public string Name => throw new NotImplementedException();

    public ILocking Create() => throw new NotImplementedException();
    public IDistributedLocking Create(string owner) => throw new NotImplementedException();
    IAtomicLocking ILock<IAtomicLocking>.Create() => throw new NotImplementedException();

    private sealed class Locking
        : IAtomicLocking,
            IDistributedLocking
    {
        public bool IsAcquired => throw new NotImplementedException();

        public string LockName => throw new NotImplementedException();

        public string Owner => throw new NotImplementedException();

        public TimeSpan TimeToLive => throw new NotImplementedException();


        private readonly AtomicLock _lock = null!;


        public void Acquire(TimeSpan expiresIn, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask AcquireAsync(TimeSpan expiresIn, CancellationToken cancellationToken)
        {
            var keeper = _lock._keeper;
            keeper.createAsync("path", [], )
        }
        public ValueTask<bool> IsAcquiredAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public void Release() => throw new NotImplementedException();
        public ValueTask ReleaseAsync() => throw new NotImplementedException();

        public bool TryAcquire(TimeSpan expiresIn, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask<bool> TryAcquireAsync(TimeSpan expiresIn, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
