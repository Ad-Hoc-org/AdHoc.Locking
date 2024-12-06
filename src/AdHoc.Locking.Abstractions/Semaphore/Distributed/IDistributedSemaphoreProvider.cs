// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking.Abstractions;
public interface IDistributedSemaphoreProvider
    : ISemaphoreProvider,
        IDistributedLockProvider<IDistributedSemaphore>
{
    ILock ILockProvider.GetLock(string name) =>
        ((ILockProvider<IDistributedSemaphore>)this).GetLock(name);

    ISemaphore ILockProvider<ISemaphore>.GetLock(string name) =>
        ((ILockProvider<IDistributedSemaphore>)this).GetLock(name);


    ValueTask SetSemaphoreCountAsync(string? name, int count, CancellationToken cancellationToken);
}
