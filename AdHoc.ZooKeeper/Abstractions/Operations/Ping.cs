// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers;
using AdHoc.ZooKeeper.Abstractions;

namespace AdHoc.ZooKeeper;
public sealed class Ping
    : IZooKeeperOperation<ErrorCode>
{
    public const int RequestID = -2;


    internal static readonly ReadOnlyMemory<byte> _Header = new byte[] {
        0, 0, 0, 8, // Length
        255,255, 255, 254, // XID
        0, 0, 0, 11 // OpCode
    };

    internal static ReadOnlyMemory<byte> _Connection => _Header.Slice(4, 4);
    internal static ReadOnlyMemory<byte> _Operation => _Header.Slice(8, 4);


    internal Ping() { }


    public void WriteRequest(IBufferWriter<byte> writer, Action<IBufferWriter<byte>, OperationCode> writeRequestID) =>
        writer.Write(_Header.Span);

    public ErrorCode ReadResponse(ZooKeeperResponse response) =>
        response.Error;

}

public static partial class Operations
{
    public static Ping Ping { get; } = new Ping();

    public static Task<ErrorCode> PingAsync(
        this IZooKeeper zooKeeper,
        CancellationToken cancellationToken
    ) =>
        zooKeeper.ExecuteAsync(Ping, cancellationToken);
}
