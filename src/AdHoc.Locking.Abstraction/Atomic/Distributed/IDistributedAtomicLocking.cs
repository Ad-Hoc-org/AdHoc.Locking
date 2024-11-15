// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking.Abstraction;

public interface IDistributedAtomicLocking
    : IAtomicLocking,
        IDistributedLocking
{
    ValueTask<bool> IsAcquiredAsync(CancellationToken cancellationToken);
}
