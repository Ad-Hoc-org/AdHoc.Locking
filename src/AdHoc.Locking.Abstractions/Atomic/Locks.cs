namespace AdHoc.Locking.Abstractions;
public static partial class Locks
{

    public static IAtomicLock GetAtomic(
        this ILockProvider provider,
        string name
    ) =>
        provider is IAtomicLockProvider atomic ? atomic.GetAtomic(name)
        : provider is IGenericLockProvider generic ? generic.GetLock<IAtomicLock>(name)
        : throw new NotSupportedException($"Provider '{provider}' aren't supporting '{typeof(IAtomicLock)}' with name '{name}'.");

}
