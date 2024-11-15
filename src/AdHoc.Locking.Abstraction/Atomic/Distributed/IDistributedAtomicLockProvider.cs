// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking.Abstraction;
public interface IDistributedAtomicLockProvider
    : IAtomicLockProvider,
        IDistributedLockProvider<IDistributedAtomicLock>
{
    ILock ILockProvider.GetLock(string name) =>
        ((ILockProvider<IDistributedAtomicLock>)this).GetLock(name);
    IAtomicLock ILockProvider<IAtomicLock>.GetLock(string name) =>
        ((ILockProvider<IDistributedAtomicLock>)this).GetLock(name);
}
