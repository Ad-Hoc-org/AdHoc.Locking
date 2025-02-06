// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers.Binary;

namespace AdHoc.ZooKeeper.Abstractions;
public static partial class Operations
{
    public const int BooleanSize = 1;
    public const int Int32Size = 4;
    public const int Int64Size = 8;

    public const int LengthSize = Int32Size;
    public const int RequestSize = Int32Size;
    public const int OperationSize = Int32Size;
    public const int RequestHeaderSize = LengthSize + RequestSize + OperationSize;

    public const int TransactionSize = Int64Size;
    public const int StatusSize = Int32Size;
    public const int MinimalResponseLength = RequestSize + TransactionSize + StatusSize;

    public const int VersionSize = Int32Size;

    public const int TimestampSize = Int64Size;


    public static void ValidateRequest(ReadOnlySpan<byte> request)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(request.Length, LengthSize + RequestSize + OperationSize);
        var length = BinaryPrimitives.ReadInt32BigEndian(request);
        ArgumentOutOfRangeException.ThrowIfNotEqual(length, request.Length - LengthSize);
    }

    public static int GetRequest(ZooKeeperOperation operation, ref int previousRequest)
    {
        if (operation == ZooKeeperOperation.Ping)
            return PingOperation.Request;

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


    public static int Write(Span<byte> destination, int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(destination, value);
        return 4;
    }

    public static int Write(Span<byte> destination, long value)
    {
        BinaryPrimitives.WriteInt64BigEndian(destination, value);
        return 8;
    }

    public static int Write(Span<byte> destination, ReadOnlySpan<byte> buffer)
    {
        Write(destination, buffer.Length);
        buffer.CopyTo(destination.Slice(LengthSize));
        return LengthSize + buffer.Length;
    }


    public static int ReadInt32(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadInt32BigEndian(source);

    public static long ReadInt64(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadInt64BigEndian(source);

    public static ReadOnlySpan<byte> ReadBuffer(ReadOnlySpan<byte> source, out int size)
    {
        int length = ReadInt32(source);
        size = length + LengthSize;
        return source.Slice(LengthSize, length);
    }

    public static DateTimeOffset ReadTimestamp(ReadOnlySpan<byte> source) =>
        DateTimeOffset.FromUnixTimeMilliseconds(ReadInt64(source));

}
