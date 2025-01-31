// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper;

[Serializable]
public class ZooKeeperException
    : IOException
{
    public ZooKeeperException() { }
    public ZooKeeperException(string message) : base(message) { }
    public ZooKeeperException(string message, Exception inner) : base(message, inner) { }
}
