// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking.Abstractions;

public interface IDistributedSemaphoreLocking
    : ISemaphoreLocking,
        IDistributedLocking
{


    ValueTask<int> GetSemaphoreCountAsync(CancellationToken cancellationToken);

    ValueTask<int> GetTotalAcquiredCountAsync(CancellationToken cancellationToken);

    ValueTask<int> GetAcquiredCountAsync(CancellationToken cancellationToken);


    bool TryAcquire(int requiredCount, TimeSpan expiresIn, CancellationToken cancellationToken = default);

    bool ISemaphoreLocking.TryAcquire(int requiredCount, CancellationToken cancellationToken) =>
        TryAcquire(requiredCount, TimeToLive, cancellationToken);

    bool IDistributedLocking.TryAcquire(TimeSpan expiresIn, CancellationToken cancellationToken) =>
        TryAcquire(1, expiresIn, cancellationToken);

    bool ILocking.TryAcquire(CancellationToken cancellationToken) =>
        TryAcquire(1, TimeToLive, cancellationToken);


    ValueTask<bool> TryAcquireAsync(int requiredCount, TimeSpan expiresIn, CancellationToken cancellationToken);

    ValueTask<bool> ISemaphoreLocking.TryAcquireAsync(int requiredCount, CancellationToken cancellationToken) =>
        TryAcquireAsync(requiredCount, TimeToLive, cancellationToken);

    ValueTask<bool> IDistributedLocking.TryAcquireAsync(TimeSpan expiresIn, CancellationToken cancellationToken) =>
        TryAcquireAsync(1, expiresIn, cancellationToken);

    ValueTask<bool> ILocking.TryAcquireAsync(CancellationToken cancellationToken) =>
        TryAcquireAsync(1, TimeToLive, cancellationToken);



    void Acquire(int requiredCount, TimeSpan expiresIn, CancellationToken cancellationToken = default);

    void ISemaphoreLocking.Acquire(int requiredCount, CancellationToken cancellationToken) =>
        Acquire(requiredCount, TimeToLive, cancellationToken);

    void IDistributedLocking.Acquire(TimeSpan expiresIn, CancellationToken cancellationToken) =>
        Acquire(1, expiresIn, cancellationToken);

    void ILocking.Acquire(CancellationToken cancellationToken) =>
        Acquire(1, TimeToLive, cancellationToken);


    ValueTask AcquireAsync(int requiredCount, TimeSpan expiresIn, CancellationToken cancellationToken);

    ValueTask ISemaphoreLocking.AcquireAsync(int requiredCount, CancellationToken cancellationToken) =>
        AcquireAsync(requiredCount, TimeToLive, cancellationToken);

    ValueTask IDistributedLocking.AcquireAsync(TimeSpan expiresIn, CancellationToken cancellationToken) =>
        AcquireAsync(1, expiresIn, cancellationToken);

    ValueTask ILocking.AcquireAsync(CancellationToken cancellationToken) =>
        AcquireAsync(1, TimeToLive, cancellationToken);


}
