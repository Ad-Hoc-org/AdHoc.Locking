// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Text;
using static AdHoc.ZooKeeper.Abstractions.CreateOperation;
using static AdHoc.ZooKeeper.Abstractions.Operations;

namespace AdHoc.ZooKeeper.Abstractions;
public sealed record CreateOperation
    : IZooKeeperOperation<Result>
{
    private enum ModeFlag : int
    {
        Persistent = 0,
        Ephemeral = 1,
        Sequential = 1 << 1,
        PersistentSequential = Persistent | Sequential,
        EphemeralSequential = Ephemeral | Sequential,
        Container = 1 << 2,
        TimeToLive = 5,
        PersistentWithTimeToLive = Persistent | TimeToLive,
        EphemeralWithTimeToLive = Ephemeral | TimeToLive,
    }
    private const int FlagSize = 4;


    private static readonly ReadOnlyMemory<byte> _Operation = new byte[] { 0, 0, 0, 1 };


    public ZooKeeperPath Path { get; }

    public ReadOnlyMemory<byte> Data { get; }


    private readonly ModeFlag _mode;
    public bool IsPersistent => !_mode.HasFlag(ModeFlag.Ephemeral | ModeFlag.Container);
    public bool IsEphemeral => _mode.HasFlag(ModeFlag.Ephemeral);
    public bool IsContainer => _mode.HasFlag(ModeFlag.Container);
    public bool IsSequential => _mode.HasFlag(ModeFlag.Sequential);

    public TimeSpan? TimeToLive { get; private init; }


    private CreateOperation(ZooKeeperPath path, ReadOnlyMemory<byte> data, ModeFlag mode)
    {
        path.Validate();
        Path = path;
        Data = data;
        _mode = mode;
    }

    public void WriteRequest(in ZooKeeperContext context)
    {
        var writer = context.Writer;
        var buffer = writer.GetSpan(RequestHeaderSize + Path.GetMaxSize(context.Root) + LengthSize + Data.Length + FlagSize);
        int size = LengthSize;

        size += Write(buffer.Slice(size), context.GetRequest(ZooKeeperOperation.Create));

        _Operation.Span.CopyTo(buffer.Slice(size));
        size += OperationSize;

        size += Path.Write(buffer.Slice(size), context.Root);

        size += Write(buffer.Slice(size), Data.Span);

        // TODO ACL
        size += Write(buffer.Slice(size), 1);
        size += Write(buffer.Slice(size), (int)ZooKeeperPermission.All);
        size += Write(buffer.Slice(size), "world".Length);
        size += Encoding.UTF8.GetBytes("world", buffer.Slice(size));
        size += Write(buffer.Slice(size), "anyone".Length);
        size += Encoding.UTF8.GetBytes("anyone", buffer.Slice(size));

        size += Write(buffer.Slice(size), (int)_mode);

        Write(buffer, size - LengthSize);
        writer.Advance(size);
    }

    public Result ReadResponse(in ZooKeeperResponse response, IZooKeeperWatcher? watcher)
    {
        if (response.Status == ZooKeeperStatus.NodeExists)
            return new((response.Root + Path).Absolute(), true);

        response.ThrowIfError();

        return new(
            ZooKeeperPath.Read(response.Data, out _),
            false
        );
    }


    public static CreateOperation Create(ZooKeeperPath path, ReadOnlyMemory<byte> data = default) =>
        new(path, data, ModeFlag.Persistent);

    public static CreateOperation CreateEphemeral(ZooKeeperPath path, ReadOnlyMemory<byte> data = default) =>
        new(path, data, ModeFlag.Ephemeral);


    public readonly record struct Result(
        ZooKeeperPath Path,
        bool AlreadyExisted
    );
}

public static partial class Operations
{

    public static Task<Result> CreateAsync(
        this IZooKeeper zooKeeper,
        ZooKeeperPath path,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken
    ) =>
        zooKeeper.ExecuteAsync(Create(path, data), cancellationToken);

    public static Task<Result> CreateAsync(
        this IZooKeeper zooKeeper,
        ZooKeeperPath path,
        CancellationToken cancellationToken
    ) =>
        zooKeeper.ExecuteAsync(Create(path), cancellationToken);


    public static Task<Result> CreateEphemeralAsync(
        this IZooKeeper zooKeeper,
        ZooKeeperPath path,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken
    ) =>
        zooKeeper.ExecuteAsync(CreateEphemeral(path, data), cancellationToken);

    public static Task<Result> CreateEphemeralAsync(
        this IZooKeeper zooKeeper,
        ZooKeeperPath path,
        CancellationToken cancellationToken
    ) =>
        zooKeeper.ExecuteAsync(CreateEphemeral(path), cancellationToken);

}
