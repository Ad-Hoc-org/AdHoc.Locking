// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking.Abstractions;
public interface IDistributedSemaphore
    : ISemaphore,
        IDistributedLock<IDistributedSemaphoreLocking>
{


    ValueTask SetSemaphoreCountAsync(int count, CancellationToken cancellationToken);

    ValueTask<int> GetSemaphoreCountAsync(CancellationToken cancellationToken);


    ValueTask<int> GetAcquiredCountAsync(CancellationToken cancellationToken);


    ILocking ILock.Create() =>
        ((ILock<IDistributedSemaphoreLocking>)this).Create();

    ISemaphoreLocking ILock<ISemaphoreLocking>.Create() =>
        ((ILock<IDistributedSemaphoreLocking>)this).Create();
}
