// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using AdHoc.ZooKeeper.Abstractions;

namespace AdHoc.ZooKeeper;
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

    public static int GetRequestID(OperationCode operation, ref int previousRequest)
    {
        if (operation == OperationCode.Ping)
            return Ping.RequestID;

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
}
