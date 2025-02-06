// See https://aka.ms/new-console-template for more information
using AdHoc.ZooKeeper;
using AdHoc.ZooKeeper.Abstractions;

// TODO refactor connection to response ID
// TODO refactor error to status



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

Console.WriteLine(await client.ExistsAsync("foo", LogEvents(), cancellationToken));
await Task.Delay(1000);
Console.WriteLine(await client.CreateAsync("foo", "bar"u8.ToArray(), cancellationToken));
await Task.Delay(1000);
Console.WriteLine(await client.ExistsAsync("foo", cancellationToken));
await Task.Delay(1000);

Console.WriteLine(await client.GetDataAsync("foo", cancellationToken));
await Task.Delay(1000);
Console.WriteLine(await client.CreateAsync("foo", cancellationToken));
await Task.Delay(1000);

Console.WriteLine(await client.CreateEphemeralAsync("ephemeral", cancellationToken));
await Task.Delay(1000);
Console.WriteLine(await client.GetDataAsync("ephemeral", cancellationToken));
await Task.Delay(1000);

Console.WriteLine(await client.ExistsAsync("empty", cancellationToken));
await Task.Delay(1000);
Console.WriteLine(await client.GetDataAsync("empty", cancellationToken));
await Task.Delay(1000);

Console.WriteLine(await client.DeleteAsync("foo", cancellationToken));
await Task.Delay(1000);
Console.WriteLine(await client.CreateAsync("foo", "bar"u8.ToArray(), cancellationToken));
await Task.Delay(1000);
Console.WriteLine(await client.DeleteAsync("foo", cancellationToken));
await Task.Delay(1000);

Console.WriteLine("done");

IZooKeeperWatcher.Watch LogEvents() => (_, ev) =>
{
    var bg = Console.BackgroundColor;
    Console.BackgroundColor = ConsoleColor.DarkGreen;
    Console.WriteLine(ev);
    Console.BackgroundColor = bg;
    //await client.ExistsAsync("foo", Exists(), cancellationToken);
};
//await client.PingAsync(cancellation);
//await client.CreateAsync("/test", Encoding.UTF8.GetBytes("hello"), cancellation);

//Console.WriteLine("Done!");
