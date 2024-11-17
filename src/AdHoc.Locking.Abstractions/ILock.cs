using System.ComponentModel;

namespace AdHoc.Locking.Abstractions;
public interface ILock
{


    string Name { get; }


    ILocking Create();


}


[EditorBrowsable(EditorBrowsableState.Advanced)]
public interface ILock<TLocking>
    : ILock
    where TLocking : ILocking
{
    new TLocking Create();

    ILocking ILock.Create() =>
        Create();
}
