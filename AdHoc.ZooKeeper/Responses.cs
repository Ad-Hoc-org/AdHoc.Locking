// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT
using static AdHoc.ZooKeeper.Requests;

namespace AdHoc.ZooKeeper;
public static partial class Responses
{

    public const int DefaultPasswordSize = 16;
    public const int DefaultSessionResponseSize = LengthSize + ClientConnectionSize + TimeoutSize + SessionSize + LengthSize + DefaultPasswordSize + ReadOnlySize;
    public const int ServerConnectionSize = 8;

}
