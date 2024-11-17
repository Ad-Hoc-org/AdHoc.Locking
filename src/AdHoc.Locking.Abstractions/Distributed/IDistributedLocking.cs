// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking.Abstractions;
public interface IDistributedLocking
    : ILocking
{


    public string Owner { get; }

    public TimeSpan ExpiryInterval { get; }



    bool TryAcquire(TimeSpan expiresIn, CancellationToken cancellationToken = default);

    bool ILocking.TryAcquire(CancellationToken cancellationToken) =>
        TryAcquire(ExpiryInterval, cancellationToken);


    ValueTask<bool> TryAcquireAsync(TimeSpan expiresIn, CancellationToken cancellationToken);

    ValueTask<bool> ILocking.TryAcquireAsync(CancellationToken cancellationToken) =>
        TryAcquireAsync(ExpiryInterval, cancellationToken);



    void Acquire(TimeSpan expiresIn, CancellationToken cancellationToken = default);

    void ILocking.Acquire(CancellationToken cancellationToken) =>
        Acquire(ExpiryInterval, cancellationToken);


    ValueTask AcquireAsync(TimeSpan expiresIn, CancellationToken cancellationToken);

    ValueTask ILocking.AcquireAsync(CancellationToken cancellationToken) =>
        AcquireAsync(ExpiryInterval, cancellationToken);


}
