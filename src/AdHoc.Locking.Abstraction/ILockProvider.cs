using System.ComponentModel;

namespace AdHoc.Locking.Abstraction;
public interface ILockProvider
{


    ILock GetLock(string name);


}


[EditorBrowsable(EditorBrowsableState.Advanced)]
public interface ILockProvider<TLock>
    : ILockProvider
    where TLock : ILock
{
    new TLock GetLock(string name);

    ILock ILockProvider.GetLock(string name) =>
        GetLock(name);
}
