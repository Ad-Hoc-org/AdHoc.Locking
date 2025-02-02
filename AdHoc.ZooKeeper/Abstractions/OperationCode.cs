// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.ZooKeeper.Abstractions;
public enum OperationCode
    : int
{
    Authentication = 100,
    CloseSession = -11,
    Create = 1,
    CreateSession = -10,
    Delete = 2,
    Error = -1,
    Exists = 3,
    GetAccessControlList = 6,
    GetChildren = 8,
    GetChildren2 = 12,
    GetData = 4,
    Notification = 0,
    Ping = 11,
    SetAccessControlList = 7,
    SetData = 5,
    SetWatches = 101,
    Sync = 9
}
