// See https://aka.ms/new-console-template for more information
using AdHoc.Locking;
using AdHoc.Locking.Abstraction;

try
{
    AtomicFileLock atomic = new AtomicFileLock("/data/atomics/my-lock", TimeSpan.FromMinutes(1));
    CancellationTokenSource tokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (_, _) => tokenSource.Cancel();
    CancellationToken cancellationToken = tokenSource.Token;

    Console.WriteLine($"Instance is running...");

    using var locking = await atomic.AcquireAsync(cancellationToken);
    Console.WriteLine($"Instance acquired the lock");

    await Task.Delay(1000);

    Console.WriteLine($"Instance is releasing the lock");
    await locking.ReleaseAsync();

    Console.WriteLine($"Instance has completed");
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    throw;
}
