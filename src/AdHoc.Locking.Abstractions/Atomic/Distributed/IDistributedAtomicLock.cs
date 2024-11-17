// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking.Abstractions;
public interface IDistributedAtomicLock
    : IAtomicLock,
        IDistributedLock<IDistributedAtomicLocking>
{
    ILocking ILock.Create() =>
        ((ILock<IDistributedAtomicLocking>)this).Create();

    IAtomicLocking ILock<IAtomicLocking>.Create() =>
        ((ILock<IDistributedAtomicLocking>)this).Create();
}
