// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT
using static AdHoc.ZooKeeper.Requests;

namespace AdHoc.ZooKeeper;
public static partial class Responses
{
    public const int ErrorSize = 4;
    public const int MinimalResponseLength = RequestIDSize + ConnectionIDSize + ErrorSize;


    public const int ConnectionIDSize = 8;
    public const int DefaultPasswordSize = 16;
    public const int DefaultSessionResponseSize = LengthSize + RequestIDSize + TimeoutSize + SessionIDSize + LengthSize + DefaultPasswordSize + ReadOnlySize;

}
