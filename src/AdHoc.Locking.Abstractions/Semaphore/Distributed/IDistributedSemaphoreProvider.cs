// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking.Abstractions;
public interface IDistributedSemaphoreProvider
    : ISemaphoreProvider,
        IDistributedLockProvider<IDistributedSemaphore>
{
    ISemaphore ILockProvider<ISemaphore>.GetLock(string name) =>
        ((ILockProvider<IDistributedSemaphore>)this).GetLock(name);


    ValueTask SetSemaphoreAsync(string? name, int count, CancellationToken cancellationToken);
}
