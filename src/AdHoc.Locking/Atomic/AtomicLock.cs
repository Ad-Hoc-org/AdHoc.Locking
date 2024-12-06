namespace AdHoc.Locking;
public sealed partial class AtomicLock
    : IAtomicLock
{


    public string Name { get; }


    private Locking? _current;

    private readonly LinkedList<Locking> _queue = new();


    public AtomicLock(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }


    public IAtomicLocking Create() =>
        new Locking(this);


}
