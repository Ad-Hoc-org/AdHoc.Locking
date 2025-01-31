// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers.Binary;

namespace AdHoc.ZooKeeper;
public static partial class Requests
{
    public const int LengthSize = 4;
    public const int ClientConnectionSize = 4;
    public const int OperationSize = 4;
    public const int SessionSize = 8;


    public static void Validate(ReadOnlySpan<byte> request)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(request.Length, LengthSize + ClientConnectionSize + OperationSize);
        var length = BinaryPrimitives.ReadInt32BigEndian(request);
        ArgumentOutOfRangeException.ThrowIfNotEqual(length, request.Length - LengthSize);
    }

    public static void SetConnection(Span<byte> request, ref int previousConnection)
    {
        Validate(request);
        var operation = request.Slice(LengthSize + ClientConnectionSize, OperationSize);
        if (operation.SequenceEqual(Ping.Span))
        {
            Ping.Span.CopyTo(operation);
            return;
        }

        int oldValue, newValue;
        do
        {
            oldValue = previousConnection;
            newValue = oldValue + 1;
            if (newValue < 0)
                newValue = 1;
        } while (Interlocked.CompareExchange(ref previousConnection, newValue, oldValue) != oldValue);
        BitConverter.TryWriteBytes(request.Slice(LengthSize), newValue);
    }
}
