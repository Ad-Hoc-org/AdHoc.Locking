// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking.Abstraction;

public interface IDistributedSemaphoreLocking
    : ISemaphoreLocking,
        IDistributedLocking
{
    ValueTask<int> GetSemaphoreCountAsync(CancellationToken cancellationToken);

    ValueTask<int> GetTotalAcquiredCountAsync(CancellationToken cancellationToken);

    ValueTask<int> GetAcquiredCountAsync(CancellationToken cancellationToken);
}
