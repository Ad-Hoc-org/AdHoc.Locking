// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper;
public interface IZooKeeperResponse
    : IDisposable
{
    ReadOnlyMemory<byte> Memory { get; }
}
