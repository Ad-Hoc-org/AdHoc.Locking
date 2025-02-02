// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper.Abstractions;
public readonly ref struct ZooKeeperResponse
{
    public int RequestID { get; }
    public long ConnectionID { get; }
    public ErrorCode Error { get; }
    public ReadOnlySpan<byte> Data { get; }

    public ZooKeeperResponse(
        int requestID,
        long connectionID,
        ErrorCode error,
        ReadOnlySpan<byte> data
    )
    {
        RequestID = requestID;
        ConnectionID = connectionID;
        Error = error;
        Data = data;
    }
}
