// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper.Abstractions;
public readonly ref struct ZooKeeperResponse
{

    public ZooKeeperPath Root { get; }

    public int RequestID { get; }
    public long ConnectionID { get; }
    public ZooKeeperError Error { get; }
    public ReadOnlySpan<byte> Data { get; }

    public ZooKeeperResponse(
        ZooKeeperPath root,
        int requestID,
        long connectionID,
        ZooKeeperError error,
        ReadOnlySpan<byte> data
    )
    {
        Root = root;
        RequestID = requestID;
        ConnectionID = connectionID;
        Error = error;
        Data = data;
    }

    public void ThrowIfError()
    {
        if (Error != ZooKeeperError.Ok)
            throw ZooKeeperException.CreateResponseError(Error);
    }
}
