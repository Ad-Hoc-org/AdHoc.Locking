// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking.Abstractions;

public interface IDistributedAtomicLocking
    : IAtomicLocking,
        IDistributedLocking
{
    ValueTask<bool> IsAcquiredAsync(CancellationToken cancellationToken);
}
