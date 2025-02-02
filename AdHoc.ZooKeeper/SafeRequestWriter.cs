// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Buffers.Binary;
using static AdHoc.ZooKeeper.Operations;

namespace AdHoc.ZooKeeper;
internal class SafeRequestWriter
    : IBufferWriter<byte>
{
    private readonly IBufferWriter<byte> _writer;

    public int Length { get; private set; } = -1;
    public int RequestID { get; private set; }
    public int Size { get; private set; }

    public bool IsCompleted => Length == Size;
    public bool IsPing { get; private set; }


    internal SafeRequestWriter(IBufferWriter<byte> writer)
    {
        _writer = writer;
        this.Write(GetSpan(4));
    }

    public void Advance(int count)
    {
        Size += count;

        if (Length == -1)
        {
            if (Size < RequestHeaderSize)
                return;

            var header = GetSpan(RequestHeaderSize);
            Length = BinaryPrimitives.ReadInt32BigEndian(header);
            if (Size >= Length)
                throw ZooKeeperException.CreateInvalidRequestSize(Length, Size);

            if (Length == RequestHeaderSize
                && header.Slice(LengthSize + RequestIDSize).SequenceEqual(Ping._Operation.Span)
            )
            {
                IsPing = true;
                RequestID = Ping.RequestID;
                return; // don't flush ping
            }

            RequestID = BinaryPrimitives.ReadInt32BigEndian(header.Slice(LengthSize));
            _writer.Advance(Size);
            return;
        }

        if (Size >= Length)
            throw ZooKeeperException.CreateInvalidRequestSize(Length, Size);
        _writer.Advance(count);
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (Length == -1)
            return GetMemory(sizeHint + Size).Slice(Size);
        return _writer.GetMemory(sizeHint);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (Length == -1)
            return GetSpan(sizeHint + Size).Slice(Size);
        return _writer.GetSpan(sizeHint);
    }

}
