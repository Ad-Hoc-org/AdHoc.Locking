// See https://aka.ms/new-console-template for more information
using AdHoc.ZooKeeper;

//try
//{
//    AtomicFileLock atomic = new AtomicFileLock("/data/atomics/my-lock", TimeSpan.FromMinutes(1));
//    CancellationTokenSource tokenSource = new CancellationTokenSource();
//    Console.CancelKeyPress += (_, _) => tokenSource.Cancel();
//    CancellationToken cancellationToken = tokenSource.Token;

//    Console.WriteLine($"Instance is running...");

//    using var locking = await atomic.AcquireAsync(cancellationToken);
//    Console.WriteLine($"Instance acquired the lock");

//    await Task.Delay(1000);

//    Console.WriteLine($"Instance is releasing the lock");
//    await locking.ReleaseAsync();

//    Console.WriteLine($"Instance has completed");
//}
//catch (Exception ex)
//{
//    Console.WriteLine(ex);
//    throw;
//}

CancellationToken cancellationToken = default;
await using var client = new ZooKeeperClient("localhost", 2181);
await client.PingAsync(cancellationToken);
await client.PingAsync(cancellationToken);
await client.PingAsync(cancellationToken);

Console.WriteLine("done");
//await client.PingAsync(cancellation);
//await client.CreateAsync("/test", Encoding.UTF8.GetBytes("hello"), cancellation);

//Console.WriteLine("Done!");
