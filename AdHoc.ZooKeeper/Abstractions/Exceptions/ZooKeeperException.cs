// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper;

[Serializable]
public class ZooKeeperException
    : Exception
{
    public ZooKeeperException() { }
    public ZooKeeperException(string? message) : base(message) { }
    public ZooKeeperException(string? message, Exception? inner) : base(message, inner) { }


    public static TimeoutException CreateTimeout(string? session, Exception? innerException = null) =>
        new($"Session '0x{session}' has timed out.", innerException)
        {
            SessionID = session
        };

    public static InvalidRequestException CreateInvalidRequestSize(int length, int size) =>
        new InvalidRequestException($"Request length is {length} but {size} was written.");
}
