// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Buffers.Binary;
using AdHoc.ZooKeeper.Abstractions;

namespace AdHoc.ZooKeeper.Abstractions;
public static partial class Operations
{
    public const int LengthSize = 4;
    public const int RequestIDSize = 4;
    public const int OperationSize = 4;
    public const int RequestHeaderSize = LengthSize + RequestIDSize + OperationSize;


    public const int ConnectionIDSize = 8;
    public const int ErrorSize = 4;
    public const int MinimalResponseLength = RequestIDSize + ConnectionIDSize + ErrorSize;

    public static void ValidateRequest(ReadOnlySpan<byte> request)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(request.Length, LengthSize + RequestIDSize + OperationSize);
        var length = BinaryPrimitives.ReadInt32BigEndian(request);
        ArgumentOutOfRangeException.ThrowIfNotEqual(length, request.Length - LengthSize);
    }

    public static int GetRequestID(ZooKeeperOperation operation, ref int previousRequest)
    {
        if (operation == ZooKeeperOperation.Ping)
            return PingOperation.RequestID;

        int oldValue, newValue;
        do
        {
            oldValue = previousRequest;
            newValue = oldValue + 1;
            if (newValue < 0)
                newValue = 1;
        } while (Interlocked.CompareExchange(ref previousRequest, newValue, oldValue) != oldValue);
        return newValue;
    }


    internal static int Write(Span<byte> destination, int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(destination, value);
        return 4;
    }
    internal static int Write(Span<byte> destination, long value)
    {
        BinaryPrimitives.WriteInt64BigEndian(destination, value);
        return 8;
    }

    internal static void Write(IBufferWriter<byte> writer, int value)
    {
        var buffer = writer.GetSpan(4);
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        writer.Advance(LengthSize);
    }
    internal static void Write(IBufferWriter<byte> writer, long value)
    {
        var buffer = writer.GetSpan(8);
        BinaryPrimitives.WriteInt64BigEndian(buffer, value);
        writer.Advance(LengthSize);
    }
}
