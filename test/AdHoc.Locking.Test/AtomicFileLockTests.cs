using AdHoc.Locking.Abstractions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;

namespace AdHoc.Locking.Test;

public class AtomicFileLockTests
{


    private const string _LockName = "my-lock";
    private const string _OtherLockName = "other-lock";


    [Fact]
    public async Task TestMultipleProcesses()
    {
        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory("../../../../..")
            .WithDockerfile("test/AdHoc.Locking.AtomicFileLock/Dockerfile")
            .Build();
        await image.CreateAsync();


        var atomic = new AtomicFileLock("atomics/my-lock", TimeSpan.FromMinutes(5));

        var builder = new ContainerBuilder()
            .WithImage(image)
            .WithBindMount(Path.GetFullPath("atomics/"), "/data/atomics/")
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole());

        IContainer[] containers = Enumerable.Range(0, 25)
            .Select(i => builder.Build())
            .ToArray();

        await using var locking = await atomic.AcquireAsync(CancellationToken.None);


        await Task.WhenAll(containers.Select(
            container => container.StartAsync()
        ));

        containers.All(container => container.State == TestcontainersStates.Running)
            .Should().BeTrue();

        TimeSpan lockDelay = TimeSpan.FromMilliseconds(1000);
        float tolerance = 2F;
        CancellationTokenSource cancellationTokenSource = new(25 * lockDelay * tolerance);
        var exited = Task.WhenAll(containers.Select(async container =>
            (await container.GetExitCodeAsync(cancellationTokenSource.Token)).Should().Be(0)
        ));

        await locking.ReleaseAsync();
        await exited;
    }



    [Fact]
    public async Task TestTryAcquireAsync()
    {
        CancellationToken cancellationToken = CancellationToken.None;
        var atomic = new AtomicFileLock(_LockName, TimeSpan.FromSeconds(10));

        using var locking = atomic.Create("myOwner");
        (await locking.TryAcquireAsync(cancellationToken)).Should().BeTrue();

        using var otherLocking = atomic.Create();
        (await otherLocking.TryAcquireAsync(cancellationToken)).Should().BeFalse();

        var newAtomic = new AtomicFileLock(_LockName, TimeSpan.FromSeconds(5));
        using var newOwnedLocking = newAtomic.Create(locking.Owner);
        (await newOwnedLocking.TryAcquireAsync(cancellationToken)).Should().BeTrue();

        (await otherLocking.TryAcquireAsync(cancellationToken)).Should().BeFalse();
        await Task.Delay(locking.TimeToLive * 1.25, cancellationToken); // await expiration

        (await otherLocking.TryAcquireAsync(cancellationToken)).Should().BeTrue();
        (await newOwnedLocking.TryAcquireAsync(cancellationToken)).Should().BeFalse();
        (await locking.TryAcquireAsync(cancellationToken)).Should().BeFalse();
    }


}
