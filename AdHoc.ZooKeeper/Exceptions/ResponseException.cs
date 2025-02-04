// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper.Abstractions;

[Serializable]
public class ResponseException : ZooKeeperException
{

    public ZooKeeperError Error { get; }

    public ResponseException(ZooKeeperError error) => Error = error;
    public ResponseException(ZooKeeperError error, string message) : base(message) => Error = error;
    public ResponseException(ZooKeeperError error, string message, Exception inner) : base(message, inner) => Error = error;
}
