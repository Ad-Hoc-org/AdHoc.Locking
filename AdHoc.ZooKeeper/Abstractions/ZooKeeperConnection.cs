// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper.Abstractions;
public sealed record ZooKeeperConnection
{
    public const int DefaultPort = 2181;

    public readonly record struct Node(string Host, int Port = DefaultPort);
    public readonly record struct Authentication(string Scheme, byte[] Data);


    //public IEnumerable<Node> Nodes { get; }

    //public IEnumerable<Authentication> Authentications
    //{
    //    get;
    //    init
    //    {

    //    }
    //}


    //public TimeSpan SessionTimeout
    //{
    //    get;
    //    init
    //    {
    //        ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
    //        field = value;
    //    }
    //}


    //public ZooKeeperPath Root { get; init; }


    //public ZooKeeperConnection(
    //    params IEnumerable<Node> nodes
    //)
    //{

    //}




}
