// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper;

[Serializable]
public class TimeoutException
    : ConnectionException
{
    public string? SessionID { get; init; }

    public TimeoutException() { }
    public TimeoutException(string? message) : base(message) { }
    public TimeoutException(string? message, Exception? inner) : base(message, inner) { }
}
