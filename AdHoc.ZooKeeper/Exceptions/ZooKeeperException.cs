// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper.Abstractions;

[Serializable]
public class ZooKeeperException
    : Exception
{
    public ZooKeeperException() { }
    public ZooKeeperException(string? message) : base(message) { }
    public ZooKeeperException(string? message, Exception? inner) : base(message, inner) { }


    public static SessionExpiredException CreateSessionExpired(string? session, Exception? innerException = null) =>
        new($"Session '0x{session}' has timed out.", innerException)
        {
            Session = session
        };

    public static InvalidRequestException CreateInvalidRequestSize(int length, int size) =>
        new($"Request length is {length} but {size} was written.");

    public static ResponseException CreateResponseError(ZooKeeperError error) =>
        new(error, $"Response indicate an error: {error}");
}
