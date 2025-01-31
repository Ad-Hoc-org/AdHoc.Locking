// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper;
public static partial class Requests
{
    public const int PingSize = LengthSize + ClientConnectionSize + OperationSize;

    public static readonly ReadOnlyMemory<byte> Ping = new byte[] { 0, 0, 0, 11 };

    public static void CreatePing(Span<byte> buffer)
    {
        buffer.Slice(0, 3).Clear();
        buffer[3] = PingSize - 4;
        // 4, 4 connection - will be set from client (usually always 0xfffffe)
        Ping.Span.CopyTo(buffer.Slice(LengthSize + ClientConnectionSize));
    }
}
