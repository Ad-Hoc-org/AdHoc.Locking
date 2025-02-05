// See https://aka.ms/new-console-template for more information
using AdHoc.ZooKeeper;
using AdHoc.ZooKeeper.Abstractions;

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
Console.WriteLine(await client.CreateAsync("foo", "bar"u8.ToArray(), cancellationToken));
Console.WriteLine(await client.ExistsAsync("foo", cancellationToken));
Console.WriteLine(await client.GetDataAsync("foo", cancellationToken));
Console.WriteLine(await client.CreateAsync("foo", cancellationToken));
Console.WriteLine(await client.CreateEphemeralAsync("ephemeral", cancellationToken));
Console.WriteLine(await client.GetDataAsync("ephemeral", cancellationToken));
Console.WriteLine(await client.ExistsAsync("empty", cancellationToken));
Console.WriteLine(await client.GetDataAsync("empty", cancellationToken));
Console.WriteLine(await client.DeleteAsync("foo", cancellationToken));
Console.WriteLine(await client.DeleteAsync("foo", cancellationToken));



Console.WriteLine("done");
//await client.PingAsync(cancellation);
//await client.CreateAsync("/test", Encoding.UTF8.GetBytes("hello"), cancellation);

//Console.WriteLine("Done!");
