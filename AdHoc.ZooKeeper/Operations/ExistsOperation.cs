// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using static AdHoc.ZooKeeper.Abstractions.ExistsOperation;
using static AdHoc.ZooKeeper.Abstractions.Operations;

namespace AdHoc.ZooKeeper.Abstractions;
public sealed record ExistsOperation
    : IZooKeeperOperation<Result>
{
    private static readonly ReadOnlyMemory<byte> _Operation = new byte[] { 0, 0, 0, 3 };

    public ZooKeeperPath Path { get; }

    private ExistsOperation(ZooKeeperPath path)
    {
        path.Validate();
        Path = path;
    }

    public void WriteRequest(in ZooKeeperContext context)
    {
        var writer = context.Writer;
        var buffer = writer.GetSpan(RequestHeaderSize + Path.GetMaxSize(context.Root));
        int size = LengthSize;

        size += Write(buffer.Slice(size), context.GetRequest(ZooKeeperOperation.Exists));

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

        var node = ZooKeeperNode.ReadStats(
            response.Data,
            (response.Root + Path).Absolute(),
            out _
        );
        return new(node);
    }

    public static ExistsOperation Create(ZooKeeperPath path) =>
        new(path);

    public readonly record struct Result(
        ZooKeeperNode? Node
    );
}

public static partial class Operations
{
    public static Task<Result> ExistsAsync(
        this IZooKeeper zooKeeper,
        ZooKeeperPath path,
        CancellationToken cancellationToken
    ) =>
        zooKeeper.ExecuteAsync(Create(path), cancellationToken);
}
