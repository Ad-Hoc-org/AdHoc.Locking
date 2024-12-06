namespace AdHoc.Locking;

public sealed partial class Semaphore
    : ISemaphore
{


    public string Name { get; }

    public int SemaphoreCount
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            field = value;
        }
    }

    public int AcquiredCount { get; private set; }


    private readonly Dictionary<Locking, int> _acquired;
    private readonly LinkedList<Acquisition> _queue;


    public Semaphore(string name, int count)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
        SemaphoreCount = count;
        _acquired = [];
        _queue = [];
    }


    public ISemaphoreLocking Create() =>
        new Locking(this);


    private sealed record Acquisition(int Count, Locking Semaphore);
}
