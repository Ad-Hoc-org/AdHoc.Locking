// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers;
using static AdHoc.ZooKeeper.Abstractions.Operations;

namespace AdHoc.ZooKeeper.Abstractions;
public sealed record PingOperation
    : IZooKeeperOperation<ZooKeeperStatus>
{
    public const int Request = -2;


    internal static readonly ReadOnlyMemory<byte> _Header = new byte[] {
        0, 0, 0, 8, // Length
        255,255, 255, 254, // XID
        0, 0, 0, 11 // OpCode
    };

    internal static ReadOnlyMemory<byte> _Request => _Header.Slice(LengthSize, RequestSize);
    internal static ReadOnlyMemory<byte> _Operation => _Header.Slice(LengthSize + RequestSize, OperationSize);


    internal PingOperation() { }


    public void WriteRequest(in ZooKeeperContext context) =>
        context.Writer.Write(_Header.Span);

    public ZooKeeperStatus ReadResponse(in ZooKeeperResponse response, IZooKeeperWatcher? watcher) =>
        response.Status;

}

public static partial class Operations
{
    public static PingOperation Ping { get; } = new PingOperation();

    public static Task<ZooKeeperStatus> PingAsync(
        this IZooKeeper zooKeeper,
        CancellationToken cancellationToken
    ) =>
        zooKeeper.ExecuteAsync(Ping, cancellationToken);
}
