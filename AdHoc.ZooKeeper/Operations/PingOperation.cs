// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers;
using static AdHoc.ZooKeeper.Abstractions.Operations;

namespace AdHoc.ZooKeeper.Abstractions;
public sealed record PingOperation
    : IZooKeeperOperation<ZooKeeperError>
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

    public ZooKeeperError ReadResponse(in ZooKeeperResponse response) =>
        response.Error;

}

public static partial class Operations
{
    public static PingOperation Ping { get; } = new PingOperation();

    public static Task<ZooKeeperError> PingAsync(
        this IZooKeeper zooKeeper,
        CancellationToken cancellationToken
    ) =>
        zooKeeper.ExecuteAsync(Ping, cancellationToken);
}
