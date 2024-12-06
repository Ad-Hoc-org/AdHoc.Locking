// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.ComponentModel;

namespace AdHoc.Locking.Abstractions;
public interface IDistributedLock
    : ILock
{
    public TimeSpan TimeToLive { get; set; }

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
