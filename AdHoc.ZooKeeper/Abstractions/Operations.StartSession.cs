// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers.Binary;

namespace AdHoc.ZooKeeper;
public static partial class Operations
{
    public const int ProtocolVersionSize = 4;
    public const int TimeoutSize = 4;
    public const int ReadOnlySize = 1;
    public const int StartSessionSize = LengthSize + ProtocolVersionSize + ConnectionIDSize + TimeoutSize + SessionIDSize + LengthSize + ReadOnlySize;

    public static void CreateStartSession(Span<byte> buffer, int timeout, bool readOnly)
    {
        // 0, 4 length
        buffer.Slice(0, 3).Clear();
        buffer[3] = StartSessionSize - LengthSize; // length
        // 4, 4 protocol version
        // 12, 4 last zxid
        buffer.Slice(LengthSize, RequestIDSize + SessionIDSize).Clear();
        // 16, 4 timeout
        BitConverter.TryWriteBytes(buffer.Slice(16, TimeoutSize), BinaryPrimitives.ReverseEndianness(timeout));
        // 20, 8 session
        // 28, 4 password size
        buffer.Slice(20, SessionIDSize + LengthSize).Clear();
        buffer[32] = (byte)(readOnly ? 1 : 0); // 33 readonly
    }
}
