// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper.Abstractions;
public interface IZooKeeperResponse
    : IDisposable
{
    ReadOnlyMemory<byte> Memory { get; }
}
