// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper.Abstractions;
public interface IZooKeeper
    : IAsyncDisposable
{
    public Task<IZooKeeperResponse> SendAsync(Memory<byte> request, CancellationToken cancellationToken);
}
