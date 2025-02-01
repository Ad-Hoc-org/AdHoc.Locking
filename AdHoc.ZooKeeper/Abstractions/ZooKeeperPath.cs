// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper;
public readonly record struct ZooKeeperPath(string value)
{
    public string Value { get; } = value;
}
