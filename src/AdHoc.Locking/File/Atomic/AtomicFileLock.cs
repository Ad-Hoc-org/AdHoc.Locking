// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking;
public sealed partial class AtomicFileLock
    : IDistributedAtomicLock
{


    private const string _LockFileName = "atomic";


    public string Name { get; }


    private readonly string _lockFile;

    public string LockPath { get; }


    public TimeSpan TimeToLive
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
            field = value;
        }
    }


    public AtomicFileLock(string lockPath, TimeSpan timeToLive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lockPath);
        LockPath = Path.GetFullPath(lockPath);
        _lockFile = Path.Combine(LockPath, _LockFileName);
        Name = Path.GetFileName(lockPath);
        TimeToLive = timeToLive;
    }


    public IDistributedAtomicLocking Create(string owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        return new Locking(this, owner);
    }

    public IDistributedAtomicLocking Create() =>
        Create(Guid.NewGuid().ToString());

    private sealed record LockingInfo(string Owner, DateTime ExpiresAt);
}
