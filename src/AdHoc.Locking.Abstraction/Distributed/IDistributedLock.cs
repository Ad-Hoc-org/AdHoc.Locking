// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.ComponentModel;

namespace AdHoc.Locking.Abstraction;
public interface IDistributedLock
    : ILock
{
    IDistributedLocking Create(string owner);
}


[EditorBrowsable(EditorBrowsableState.Advanced)]
public interface IDistributedLock<TLocking>
    : IDistributedLock,
        ILock<TLocking>
    where TLocking : IDistributedLocking
{
    new TLocking Create(string owner);

    IDistributedLocking IDistributedLock.Create(string owner) =>
        Create(owner);
}
