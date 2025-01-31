// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking.Abstractions;

public interface IGenericLockProvider
    : ILockProvider
{
    TLock GetLock<TLock>(string name)
        where TLock : ILock;

    ILock ILockProvider.GetLock(string name) =>
        GetLock<ILock>(name);
}
