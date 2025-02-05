// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using static AdHoc.ZooKeeper.Abstractions.GetDataOperation;
using static AdHoc.ZooKeeper.Abstractions.Operations;

namespace AdHoc.ZooKeeper.Abstractions;
public sealed record GetDataOperation
    : IZooKeeperOperation<Result>
{
    private static readonly ReadOnlyMemory<byte> _Operation = new byte[] { 0, 0, 0, 4 };

    public ZooKeeperPath Path { get; }

    private GetDataOperation(ZooKeeperPath path)
    {
        path.Validate();
        Path = path;
    }

    public void WriteRequest(in ZooKeeperContext context)
    {
        var writer = context.Writer;
        var buffer = writer.GetSpan(RequestHeaderSize + Path.GetMaxSize(context.Root));
        int size = LengthSize;

        size += Write(buffer.Slice(size), context.GetRequest(ZooKeeperOperation.GetData));

        _Operation.Span.CopyTo(buffer.Slice(size));
        size += OperationSize;

        size += Path.Write(buffer.Slice(size), context.Root);

        // TODO watches
        buffer[size++] = 0;

        Write(buffer, size - LengthSize);
        writer.Advance(size);
    }

    public Result ReadResponse(in ZooKeeperResponse response)
    {
        if (response.Error == ZooKeeperError.NoNode)
            return default;

        response.ThrowIfError();

        var data = ReadBuffer(response.Data, out int pos);
        return new(
            data.ToArray(),
            ZooKeeperNode.ReadStats(
                response.Data.Slice(pos),
                (response.Root + Path).Absolute(),
                out _
            )
        );
    }

    public static GetDataOperation Create(ZooKeeperPath path) =>
        new(path);

    public readonly record struct Result(
        ReadOnlyMemory<byte> Data,
        ZooKeeperNode? Node
    );
}

public static partial class Operations
{
    public static Task<Result> GetDataAsync(
        this IZooKeeper zooKeeper,
        ZooKeeperPath path,
        CancellationToken cancellationToken
    ) =>
        zooKeeper.ExecuteAsync(Create(path), cancellationToken);
}
