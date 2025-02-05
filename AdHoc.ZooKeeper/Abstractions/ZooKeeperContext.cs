// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers;

namespace AdHoc.ZooKeeper.Abstractions;
public readonly struct ZooKeeperContext
{
    public IBufferWriter<byte> Writer { get; }

    public Func<ZooKeeperOperation, int> GetRequest { get; }

    public ZooKeeperPath Root { get; init; }


    public ZooKeeperContext(
        IBufferWriter<byte> writer,
        Func<ZooKeeperOperation, int> getRequest
    )
    {
        Writer = writer;
        GetRequest = getRequest;
    }
}
