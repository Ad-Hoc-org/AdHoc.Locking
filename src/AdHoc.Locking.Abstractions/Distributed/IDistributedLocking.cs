// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking.Abstractions;
public interface IDistributedLocking
    : ILocking
{


    public string Owner { get; }

    public TimeSpan TimeToLive { get; }



    bool TryAcquire(TimeSpan expiresIn, CancellationToken cancellationToken = default);

    bool ILocking.TryAcquire(CancellationToken cancellationToken) =>
        TryAcquire(TimeToLive, cancellationToken);


    ValueTask<bool> TryAcquireAsync(TimeSpan expiresIn, CancellationToken cancellationToken);

    ValueTask<bool> ILocking.TryAcquireAsync(CancellationToken cancellationToken) =>
        TryAcquireAsync(TimeToLive, cancellationToken);



    void Acquire(TimeSpan expiresIn, CancellationToken cancellationToken = default);

    void ILocking.Acquire(CancellationToken cancellationToken) =>
        Acquire(TimeToLive, cancellationToken);


    ValueTask AcquireAsync(TimeSpan expiresIn, CancellationToken cancellationToken);

    ValueTask ILocking.AcquireAsync(CancellationToken cancellationToken) =>
        AcquireAsync(TimeToLive, cancellationToken);


}
