// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking.Abstraction;
public interface IDistributedSemaphore
    : ISemaphore,
        IDistributedLock<IDistributedSemaphoreLocking>
{
    ISemaphoreLocking ILock<ISemaphoreLocking>.Create() =>
        ((ILock<IDistributedSemaphoreLocking>)this).Create();
}
