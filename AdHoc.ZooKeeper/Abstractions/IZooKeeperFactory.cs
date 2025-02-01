// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper.Abstractions;
public interface IZooKeeperFactory
{
    public IZooKeeper CreateZooKeeper(ZooKeeperConnection connection);
}
