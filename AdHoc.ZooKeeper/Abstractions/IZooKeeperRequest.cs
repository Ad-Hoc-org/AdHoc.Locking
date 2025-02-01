// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers;

namespace AdHoc.ZooKeeper.Abstractions;
public interface IZooKeeperRequest
{
    public ValueTask WriteAsync(IBufferWriter<byte> writer, Func<int> getConnectionID);
}
