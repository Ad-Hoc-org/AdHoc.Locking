// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking;
public class GenericLockProvider
    : IGenericLockProvider
{
    private readonly List<Func<Type, string, ILock?>> _providers = [];


    public GenericLockProvider Add(Func<Type, string, ILock?> provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (_providers)
            _providers.Add(provider);
        return this;
    }

    public GenericLockProvider Add(
        Func<Type, string, bool> predicate,
        Func<Type, string, ILock> factory
    )
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(factory);
        return Add((type, name) => predicate(type, name) ? factory(type, name) : null);
    }


    public GenericLockProvider Add<TLock>(
        Func<string, bool> predicate,
        Func<string, TLock> factory
    )
        where TLock : ILock
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(factory);
        return Add((type, name) => typeof(TLock).IsAssignableTo(type) && predicate(name) ? factory(name) : null);
    }


    public TLock GetLock<TLock>(string name)
        where TLock : ILock
    {
        ArgumentNullException.ThrowIfNull(name);
        ILock? @lock;
        foreach (var provider in _providers)
        {
            @lock = provider(typeof(TLock), name);
            if (@lock is not null)
                return (TLock)@lock;
        }

        throw new ArgumentException($"No {typeof(ILock)} '{name}' found.");
    }

}
