// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.ComponentModel;

namespace AdHoc.Locking.Abstractions;
public interface IDistributedLockProvider
    : ILockProvider
{
    new IDistributedLock GetLock(string name);

    ILock ILockProvider.GetLock(string name) =>
        GetLock(name);


    void SetTimeToLive(string? name, TimeSpan timeToLive);
}


[EditorBrowsable(EditorBrowsableState.Advanced)]
public interface IDistributedLockProvider<TLock>
    : IDistributedLockProvider,
        ILockProvider<TLock>
    where TLock : IDistributedLock
{
    IDistributedLock IDistributedLockProvider.GetLock(string name) =>
        ((ILockProvider<TLock>)this).GetLock(name);
    ILock ILockProvider.GetLock(string name) =>
        ((ILockProvider<TLock>)this).GetLock(name);
}
