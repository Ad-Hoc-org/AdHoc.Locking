// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper;

[Serializable]
public class TimeoutException
    : ZooKeeperException
{
    public TimeoutException() { }
    public TimeoutException(string message) : base(message) { }
    public TimeoutException(string message, Exception inner) : base(message, inner) { }
}
