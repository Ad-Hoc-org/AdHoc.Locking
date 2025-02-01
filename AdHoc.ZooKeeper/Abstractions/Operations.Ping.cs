// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Buffers.Binary;
using AdHoc.ZooKeeper.Abstractions;

namespace AdHoc.ZooKeeper;
public static partial class Operations
{
    public const int PingSize = LengthSize + RequestIDSize + OperationSize;

    public static readonly ReadOnlyMemory<byte> Ping = new byte[] { 0, 0, 0, 11 };
    public const int PingConnectionID = -2;
    public static readonly ReadOnlyMemory<byte> PingConnection = new byte[] { 255, 255, 255, 254 };

    public static async Task PingAsync(
        this IZooKeeper zooKeeper,
        CancellationToken cancellationToken
    )
    {
        using var pingOwner = MemoryPool<byte>.Shared.Rent(PingSize);

        var buffer = pingOwner.Memory.Span;
        buffer.Slice(0, 3).Clear();
        buffer[3] = PingSize - 4;
        // 4, 4 connection - will be set from client (usually always 0xfffffe)
        Ping.Span.CopyTo(buffer.Slice(LengthSize + RequestIDSize));

        using var response = await zooKeeper.SendAsync(pingOwner.Memory.Slice(0, PingSize), cancellationToken);

        int xid = BinaryPrimitives.ReadInt32BigEndian(response.Memory.Span);
        Console.WriteLine("xid: ");
        Console.WriteLine(xid);
        Console.WriteLine(xid.ToString("x"));

        long zxid = BinaryPrimitives.ReadInt64BigEndian(response.Memory.Span.Slice(4));
        Console.WriteLine("zxid: ");
        Console.WriteLine(zxid);
        Console.WriteLine(zxid.ToString("x"));

        int error = BinaryPrimitives.ReadInt32BigEndian(response.Memory.Span.Slice(12));
        Console.WriteLine("error: ");
        Console.WriteLine(error);
        Console.WriteLine(error.ToString("x"));
    }
}
