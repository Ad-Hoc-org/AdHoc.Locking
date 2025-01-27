// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT


using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static AdHoc.Locking.FileLocks;

namespace AdHoc.Locking;
public sealed partial class AtomicFileLock
    : IAtomicLock,
        IDistributedLock
{

    public string Name { get; }

    public string LockPath { get; }


    private readonly Func<string, TimeSpan> _timeToLive;


    public AtomicFileLock(string lockPath, TimeSpan timeToLive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lockPath);
        ArgumentOutOfRangeException.ThrowIfLessThan(timeToLive, TimeSpan.Zero);

        LockPath = Path.GetFullPath(lockPath);
        Name = Path.GetFileName(lockPath);
        _timeToLive = _ => timeToLive;
    }

    internal AtomicFileLock(string lockPath, string name, Func<string, TimeSpan> timeToLive)
    {
        LockPath = Path.GetFullPath(lockPath);
        Name = name;
        _timeToLive = timeToLive;
    }


    public IAtomicLocking Create() =>
        CreateLocking(Guid.NewGuid().ToString());

    public IDistributedLocking Create(string owner) =>
        CreateLocking(owner);

    private Locking CreateLocking(string owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ThrowIfContainsInvalidFileNameChars(owner);
        return new(this, owner);
    }



    private sealed class Locking
        : IAtomicLocking,
            IDistributedLocking
    {


        public string Owner { get; }


        public TimeSpan TimeToLive =>
            _atomic._timeToLive(_atomic.Name);

        public string LockName =>
            _atomic.Name;


        private readonly AtomicFileLock _atomic;


        internal Locking(AtomicFileLock atomic, string owner)
        {
            _atomic = atomic;
            Owner = owner;
        }


        [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Should explicit block current thread.")]
        [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Should explicit block current thread.")]
        public bool IsAcquired =>
            IsAcquiredAsync(CancellationToken.None).GetAwaiter().GetResult();

        public async ValueTask<bool> IsAcquiredAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_atomic.LockPath))
                return false;

            await using FileStream stream = await OpenAsync(readOnly: true, cancellationToken);
            LockingInfo? info = await ReadInfoAsync(stream, cancellationToken);

            return info is not null && info.Owner == Owner && info.AcquiredUntil >= DateTime.UtcNow;
        }



        [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Should explicit block current thread.")]
        [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Should explicit block current thread.")]
        public bool TryAcquire(TimeSpan expiresIn, CancellationToken cancellationToken = default) =>
            TryAcquireAsync(expiresIn, cancellationToken).GetAwaiter().GetResult();

        public async ValueTask<bool> TryAcquireAsync(TimeSpan expiresIn, CancellationToken cancellationToken)
        {
            await using (FileStream reading = await OpenAsync(readOnly: true, cancellationToken))
                if (IsAcquiredByAnother(
                    await ReadInfoAsync(reading, cancellationToken)
                ))
                    return false;

            await using FileStream writing = await OpenAsync(readOnly: false, cancellationToken);
            if (IsAcquiredByAnother(
                await ReadInfoAsync(writing, cancellationToken)
            ))
                return false;

            await WriteInfoAsync(writing, DateTime.UtcNow + expiresIn, cancellationToken);
            return true;
        }

        private bool IsAcquiredByAnother([NotNullWhen(true)] LockingInfo? info) =>
            info is not null && info.Owner != Owner && info.AcquiredUntil >= DateTime.UtcNow;


        private async ValueTask<FileStream> OpenAsync(bool readOnly, CancellationToken cancellationToken)
        {
            string path = _atomic.LockPath;
            string? directory = Path.GetDirectoryName(path);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!File.Exists(path))
                {
                    if (directory is not null && !Directory.Exists(directory))
                        try
                        {
                            Directory.CreateDirectory(directory);
                        }
                        catch (IOException) { }
                }

                try
                {
                    return readOnly ? File.Open(_atomic.LockPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)
                        : File.Open(_atomic.LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete);
                }
                catch (UnauthorizedAccessException)
                {
                    throw;
                }
                catch (IOException)
                {
                    if (Directory.Exists(path))
                        throw new InvalidDataException($"'{path}' is a directory and can't be used as a lock file.");
                    await Task.Delay(OpenInterval, cancellationToken);
                }
            }
        }

        private async ValueTask WriteInfoAsync(FileStream stream, DateTime acquiredUntil, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            stream.Position = 0;
            using StreamWriter writer = new(stream, leaveOpen: true);
            await writer.WriteLineAsync(Owner);
            await writer.WriteLineAsync(acquiredUntil.ToString("O"));
            stream.SetLength(stream.Position);
        }

        private static async ValueTask<LockingInfo?> ReadInfoAsync(FileStream stream, CancellationToken cancellationToken)
        {
            using StreamReader reader = new(stream, leaveOpen: true);
            string? owner = await reader.ReadLineAsync(cancellationToken);
            if (owner is null)
                return null;

            string? date = await reader.ReadLineAsync(cancellationToken);
            if (date is null ||
                !DateTime.TryParseExact(
                    date,
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime acquiredUntil
                )
            )
                return null;

            return new(owner, acquiredUntil);
        }


        [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Should explicit block current thread.")]
        [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Should explicit block current thread.")]
        public void Acquire(TimeSpan expiresIn, CancellationToken cancellationToken = default) =>
            AcquireAsync(expiresIn, cancellationToken).GetAwaiter().GetResult();

        public async ValueTask AcquireAsync(TimeSpan expiresIn, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                LockingInfo? info;
                await using (FileStream reading = await OpenAsync(readOnly: true, cancellationToken))
                    info = await ReadInfoAsync(reading, cancellationToken);

                if (!IsAcquiredByAnother(info))
                {
                    await using FileStream writing = await OpenAsync(readOnly: false, cancellationToken);
                    info = await ReadInfoAsync(writing, cancellationToken);
                    if (!IsAcquiredByAnother(info))
                    {
                        await WriteInfoAsync(writing, DateTime.UtcNow + expiresIn, cancellationToken);
                        return;
                    }
                }

                await Task.Delay(
                    Math.Min(MaxAcquiringInterval,
                        Math.Max(
                            (int)(info.AcquiredUntil - DateTime.UtcNow).TotalMilliseconds,
                            MinAcquiringInterval
                        )
                    ), cancellationToken
                );
            }
        }



        [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Should explicit block current thread.")]
        [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Should explicit block current thread.")]
        public void Release() =>
            ReleaseAsync().GetAwaiter().GetResult();

        public async ValueTask ReleaseAsync()
        {
            if (!File.Exists(_atomic.LockPath))
                return;

            LockingInfo? info;
            await using (FileStream reading = await OpenAsync(readOnly: true, CancellationToken.None))
                info = await ReadInfoAsync(reading, CancellationToken.None);
            if (info?.Owner != Owner)
                return;

            try
            {
                await using FileStream writing = File.Open(_atomic.LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete);
                info = await ReadInfoAsync(writing, CancellationToken.None);
                if (info?.Owner != Owner)
                    return;
                File.Delete(writing.Name);
            }
            catch (IOException) { }
        }


    }

    private sealed record LockingInfo(string Owner, DateTime AcquiredUntil);
}
