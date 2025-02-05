// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper.Abstractions;
public readonly ref struct ZooKeeperResponse
{

    public ZooKeeperPath Root { get; }

    public int RequestIdentifier { get; }
    public long ConnectionIdentifier { get; }
    public ZooKeeperError Error { get; }
    public ReadOnlySpan<byte> Data { get; }

    public ZooKeeperResponse(
        ZooKeeperPath root,
        int requestIdentifier,
        long connectionIdentifier,
        ZooKeeperError error,
        ReadOnlySpan<byte> data
    )
    {
        Root = root;
        RequestIdentifier = requestIdentifier;
        ConnectionIdentifier = connectionIdentifier;
        Error = error;
        Data = data;
    }

    public void ThrowIfError()
    {
        if (Error != ZooKeeperError.Ok)
            throw ZooKeeperException.CreateResponseError(Error);
    }
}
