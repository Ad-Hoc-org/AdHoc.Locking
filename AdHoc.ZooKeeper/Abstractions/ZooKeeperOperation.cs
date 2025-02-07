// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper.Abstractions;
public enum ZooKeeperOperation
    : int
{
    CloseSession = -11,
    CreateSession = -10,
    Error = -1,
    Notification = 0,
    Create = 1,
    Delete = 2,
    Exists = 3,
    GetData = 4,
    SetData = 5,
    GetAccessControlList = 6,
    SetAccessControlList = 7,
    Sync = 9,
    GetChildren = 8,
    GetChildren2 = 12,
    Ping = 11,
    Authentication = 100,
    SetWatches = 101,
}
