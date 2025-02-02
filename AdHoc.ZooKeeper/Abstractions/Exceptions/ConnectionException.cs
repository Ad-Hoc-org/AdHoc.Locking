// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper;

[Serializable]
public class ConnectionException : ZooKeeperException
{
    public ConnectionException() { }
    public ConnectionException(string? message) : base(message) { }
    public ConnectionException(string? message, Exception? inner) : base(message, inner) { }
}
