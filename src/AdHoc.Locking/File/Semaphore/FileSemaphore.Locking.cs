// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;

namespace AdHoc.Locking;
public sealed partial class FileSemaphore
{
    private sealed class Locking
        : IDistributedSemaphoreLocking
    {


        public int SemaphoreCount =>
            _semaphore.SemaphoreCount;

        public int TotalAcquiredCount =>
            _semaphore.AcquiredCount;


        [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Should explicit block current thread.")]
        [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Should explicit block current thread.")]
        public int AcquiredCount =>
            GetAcquiredCountAsync(CancellationToken.None).GetAwaiter().GetResult();


        public string Owner { get; }

        public TimeSpan TimeToLive =>
            _semaphore.TimeToLive;

        public string LockName =>
            _semaphore.Name;


        private readonly FileSemaphore _semaphore;
        private readonly string _lockFile;

        internal Locking(FileSemaphore semaphore, string owner)
        {
            _semaphore = semaphore;
            Owner = owner;
            _lockFile = semaphore._lockFile + "-" + owner;
        }


        public ValueTask<int> GetSemaphoreCountAsync(CancellationToken cancellationToken) =>
            _semaphore.GetSemaphoreCountAsync(cancellationToken);

        public ValueTask<int> GetTotalAcquiredCountAsync(CancellationToken cancellationToken) =>
            _semaphore.GetAcquiredCountAsync(cancellationToken);

        public async ValueTask<int> GetAcquiredCountAsync(CancellationToken cancellationToken)
        {
            await using FileStream stream = await LockFiles.OpenAsync(_lockFile, readOnly: true, cancellationToken);
            LockingInfo? info = await ReadLockingInfoAsync(stream, cancellationToken);
            return info?.Count ?? 0;
        }


        [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Should explicit block current thread.")]
        [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Should explicit block current thread.")]
        public bool TryAcquire(int requiredCount, TimeSpan expiresIn, CancellationToken cancellationToken = default) =>
            TryAcquireAsync(requiredCount, expiresIn, cancellationToken).GetAwaiter().GetResult();

        public async ValueTask<bool> TryAcquireAsync(int requiredCount, TimeSpan expiresIn, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(requiredCount, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(requiredCount, _semaphore.SemaphoreCount);

            await using FileStream locking = await LockFiles.OpenAsync(_lockFile, readOnly: false, cancellationToken);
            LockingInfo? info = await ReadLockingInfoAsync(locking, cancellationToken);
            DateTime now = DateTime.UtcNow;
            if (HasAcquired(info, requiredCount, now))
            {
                await using FileStream writing = await LockFiles.OpenAsync(_semaphore._lockFile, readOnly: false, cancellationToken);
                info = await ReadLockingInfoAsync(locking, cancellationToken);
                if (HasAcquired(info, requiredCount, now)) // check if wasn't expired - deleted
                {
                    DateTime expiresAt = now + expiresIn;
                    await WriteLockingInfoAsync(
                        locking,
                        new(
                            info.Count,
                            expiresAt < info.ExpiresAt ? info.ExpiresAt : expiresAt
                        ),
                        cancellationToken
                    );
                    return true;
                }
            }

            await using FileStream reading = await LockFiles.OpenAsync(_semaphore._lockFile, readOnly: true, cancellationToken);
            SemaphoreInfo? semaphore = await _semaphore.ReadInfoAsync(reading, isReadOnly: true, cancellationToken);
            //var acquireCount = 
        }

        private static bool HasAcquired([NotNullWhen(true)] LockingInfo? info, int count, DateTime now) =>
            info is not null && info.ExpiresAt >= now && info.Count >= count;


        [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Should explicit block current thread.")]
        [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Should explicit block current thread.")]
        public void Acquire(int requiredCount, TimeSpan expiresIn, CancellationToken cancellationToken = default) =>
            AcquireAsync(requiredCount, expiresIn, cancellationToken).GetAwaiter().GetResult();

        public ValueTask AcquireAsync(int requiredCount, TimeSpan expiresIn, CancellationToken cancellationToken) =>
            throw new NotImplementedException();


        public void Release(int remainingCount) => throw new NotImplementedException();

        public ValueTask ReleaseAsync(int remainingCount) => throw new NotImplementedException();


    }
}
