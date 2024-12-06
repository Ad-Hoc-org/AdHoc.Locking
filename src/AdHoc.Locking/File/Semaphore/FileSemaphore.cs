// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT



using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace AdHoc.Locking;
public sealed partial class FileSemaphore
    : IDistributedSemaphore
{


    private const string _LockFileName = "semaphore";
    private static readonly int _MaxCountLength = int.MaxValue.ToString().Length;


    public string Name { get; }

    public string LockPath { get; }

    private readonly string _lockFile;


    public TimeSpan TimeToLive
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
            field = value;
        }
    }


    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Should explicit block current thread.")]
    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Should explicit block current thread.")]
    public int SemaphoreCount
    {
        get => GetSemaphoreCountAsync(CancellationToken.None).GetAwaiter().GetResult();
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            SetSemaphoreCountAsync(value, CancellationToken.None).GetAwaiter().GetResult();
        }
    }


    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Should explicit block current thread.")]
    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Should explicit block current thread.")]
    public int AcquiredCount =>
        GetAcquiredCountAsync(CancellationToken.None).GetAwaiter().GetResult();



    public FileSemaphore(string lockPath, int count, TimeSpan timeToLive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lockPath);
        LockPath = Path.GetFullPath(lockPath);
        _lockFile = Path.Combine(LockPath, _LockFileName);
        Name = Path.GetFileName(lockPath);
        SemaphoreCount = count;
        TimeToLive = timeToLive;
    }


    public IDistributedSemaphoreLocking Create() =>
        Create(Guid.NewGuid().ToString());

    public IDistributedSemaphoreLocking Create(string owner)
    {
        ArgumentException.ThrowIfNullOrEmpty(owner);
        return new Locking(this, owner);
    }


    public async ValueTask SetSemaphoreCountAsync(int count, CancellationToken cancellationToken)
    {
        await using FileStream stream = await LockFiles.OpenAsync(_lockFile, readOnly: false, cancellationToken);
        using StreamWriter writer = new StreamWriter(stream, leaveOpen: true);
        await writer.WriteLineAsync(new StringBuilder(_MaxCountLength).AppendFormat($"{{0:{_MaxCountLength}}}", count), cancellationToken);
    }


    private async ValueTask SetAcquiredCountAsync(Stream stream, int count, DateTime expiresAt, CancellationToken cancellationToken)
    {
        stream.Position = _MaxCountLength; // skip semaphore count
        using StreamReader reader = new StreamReader(stream, leaveOpen: true);
        await reader.ReadLineAsync(cancellationToken); // new line chars
        using StreamWriter writer = new StreamWriter(stream, leaveOpen: true);
        await writer.WriteLineAsync(new StringBuilder(_MaxCountLength).AppendFormat($"{{0:{_MaxCountLength}}}", count), cancellationToken);
        long dateStart = stream.Position;
        string? dateLine = await reader.ReadLineAsync(cancellationToken);
        if (DateTime.TryParseExact(dateLine, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime firstExpiration)
            && firstExpiration > expiresAt)
            return;
        stream.Position = dateStart;
        await writer.WriteLineAsync(expiresAt.ToString("O", CultureInfo.InvariantCulture));
    }


    public async ValueTask<int> GetSemaphoreCountAsync(CancellationToken cancellationToken)
    {
        await using FileStream stream = await LockFiles.OpenAsync(_lockFile, readOnly: true, cancellationToken);
        SemaphoreInfo? info = await ReadInfoAsync(stream, isReadOnly: true, cancellationToken);
        return info?.AcquiredCount ?? 1;
    }

    public async ValueTask<int> GetAcquiredCountAsync(CancellationToken cancellationToken)
    {
        await using FileStream stream = await LockFiles.OpenAsync(_lockFile, readOnly: true, cancellationToken);
        SemaphoreInfo? info = await ReadInfoAsync(stream, isReadOnly: true, cancellationToken);
        return info?.AcquiredCount ?? 0;
    }


    private async ValueTask<SemaphoreInfo?> ReadInfoAsync(Stream stream, bool isReadOnly, CancellationToken cancellationToken)
    {
        using StreamReader reader = new(stream, leaveOpen: true);
        string? line = await reader.ReadLineAsync(cancellationToken);
        if (line is null)
            return null;
        if (!int.TryParse(line, out int semaphoreCount))
            return null;
        line = await reader.ReadLineAsync(cancellationToken);
        if (line is null)
            return new SemaphoreInfo(semaphoreCount, 0, default);
        if (!int.TryParse(line, out int acquiredCount))
            return null;
        line = await reader.ReadLineAsync(cancellationToken);

        DateTime now = DateTime.UtcNow;
        if (DateTime.TryParseExact(line, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime firstExpiration)
            && firstExpiration < now)
        {
            if (isReadOnly)
                stream = await LockFiles.OpenAsync(_lockFile, readOnly: false, cancellationToken);
            else
                stream.Position = 0;

            try
            {
                acquiredCount = 0;
                firstExpiration = DateTime.MaxValue;
                foreach (string file in Directory.GetFiles(LockPath, _LockFileName + "-"))
                {
                    await using var locking = await LockFiles.OpenAsync(file, readOnly: true, cancellationToken);
                    LockingInfo? info = await ReadLockingInfoAsync(locking, cancellationToken);
                    if (info is not null)
                        if (info.ExpiresAt < now)
                            File.Delete(file); // cleanup
                        else
                        {
                            if (info.ExpiresAt < firstExpiration)
                                firstExpiration = info.ExpiresAt;
                            acquiredCount += info.Count;
                        }
                }
            }
            finally
            {
                if (isReadOnly)
                    await stream.DisposeAsync();
            }
        }

        return new SemaphoreInfo(semaphoreCount, acquiredCount, firstExpiration);
    }


    private static async ValueTask<LockingInfo?> ReadLockingInfoAsync(Stream stream, CancellationToken cancellationToken)
    {
        using StreamReader reader = new(stream, leaveOpen: true);
        string? line = await reader.ReadLineAsync(cancellationToken);
        if (line is null)
            return null;
        if (!int.TryParse(line, out int count))
            return null;
        line = await reader.ReadLineAsync(cancellationToken);
        if (!DateTime.TryParseExact(line, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime expiresAt))
            return null;
        return new LockingInfo(count, expiresAt);
    }

    private static async ValueTask WriteLockingInfoAsync(Stream stream, LockingInfo info, CancellationToken cancellationToken)
    {
        using StreamWriter writer = new(stream, leaveOpen: true);
        writer.Write(info.Count);
        await writer.WriteLineAsync();
        await writer.WriteLineAsync(info.ExpiresAt.ToString("O"));
    }

    private sealed record LockingInfo(int Count, DateTime ExpiresAt);
    private sealed record SemaphoreInfo(int SemaphoreCount, int AcquiredCount, DateTime FirstExpiration);
}
