// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking.Abstractions;
public static partial class Locks
{

    public static ILocking Acquire(
        this ILock @lock,
        CancellationToken cancellationToken = default
    )
    {
        ILocking locking = @lock.Create();
        locking.Acquire(cancellationToken);
        return locking;
    }

    public static async ValueTask<ILocking> AcquireAsync(
        this ILock @lock,
        CancellationToken cancellationToken
    )
    {
        ILocking locking = @lock.Create();
        await locking.AcquireAsync(cancellationToken);
        return locking;
    }

}
