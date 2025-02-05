// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers.Binary;

namespace AdHoc.ZooKeeper.Abstractions;
public static partial class Operations
{
    public const int ProtocolVersionSize = Int32Size;
    public const int TimeoutSize = Int32Size;
    public const int SessionSize = Int64Size;
    public const int ReadOnlySize = BooleanSize;
    public const int StartSessionSize = LengthSize + ProtocolVersionSize + ConnectionSize + TimeoutSize + SessionSize + LengthSize + ReadOnlySize;

    public const int DefaultPasswordSize = 16;
    public const int DefaultSessionResponseSize = LengthSize + RequestSize + TimeoutSize + SessionSize + LengthSize + DefaultPasswordSize + ReadOnlySize;

    public static void CreateStartSession(Span<byte> buffer, int timeout, bool readOnly)
    {
        // 0, 4 length
        buffer.Slice(0, 3).Clear();
        buffer[3] = StartSessionSize - LengthSize; // length
        // 4, 4 protocol version
        // 12, 4 last zxid
        buffer.Slice(LengthSize, RequestSize + SessionSize).Clear();
        // 16, 4 timeout
        BitConverter.TryWriteBytes(buffer.Slice(16, TimeoutSize), BinaryPrimitives.ReverseEndianness(timeout));
        // 20, 8 session
        // 28, 4 password size
        buffer.Slice(20, SessionSize + LengthSize).Clear();
        buffer[32] = (byte)(readOnly ? 1 : 0); // 33 readonly
    }
}
