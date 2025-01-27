// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking.Abstractions;
public interface IDistributedLock
    : ILock
{
    IDistributedLocking Create(string owner);
}
