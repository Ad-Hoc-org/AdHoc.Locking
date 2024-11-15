using FluentAssertions;

namespace AdHoc.Locking.Test;

public class AtomicLockTests
{


    private const string _LockName = "my-lock";
    private const string _OtherLockName = "other-lock";


    [Fact]
    public void TestFactorySingleton()
    {
        var provider = new AtomicLockProvider();
        var factory = provider.GetLock(_LockName);
        factory.Should().NotBeNull();
        factory.Should().BeSameAs(provider.GetLock(_LockName));
        factory.Should().NotBeSameAs(provider.GetLock(_OtherLockName));
    }


    [Fact]
    public void TestTryAcquire()
    {
        var factory = new AtomicLock(_LockName);

        using var myLock = factory.Create();
        myLock.TryAcquire().Should().BeTrue();
        myLock.TryAcquire().Should().BeTrue();

        using var otherLock = factory.Create();
        otherLock.TryAcquire().Should().BeFalse();

        myLock.Release();
        otherLock.TryAcquire().Should().BeTrue();
        myLock.TryAcquire().Should().BeFalse();
    }


    [Fact]
    public async Task TestAcquire()
    {
        var factory = new AtomicLock(_LockName);

        await using var myLock = factory.Create();
        myLock.IsAcquired.Should().BeFalse();
        await myLock.AcquireAsync(default);
        myLock.IsAcquired.Should().BeTrue();

        // timeout only if doesn't work - faster tests
        await myLock.Invoking(async l => await l.AcquireAsync(default))
            .Should().CompleteWithinAsync(TimeSpan.FromSeconds(1));

        await using var otherLock = factory.Create();
        var otherAcquire = Task.Run(async () =>
        {
            myLock.IsAcquired.Should().BeTrue();
            await otherLock.AcquireAsync(default);
            myLock.IsAcquired.Should().BeFalse();
            otherLock.IsAcquired.Should().BeTrue();
        });

        otherLock.IsAcquired.Should().BeFalse();
        await Task.Delay(500); // wait for otherLock to arquire
        await myLock.ReleaseAsync();
        await otherAcquire;
        otherLock.IsAcquired.Should().BeTrue();
    }


    [Fact]
    public void TestRelease()
    {
        var factory = new AtomicLock(_LockName);

        using var myLock = factory.Create();
        myLock.IsAcquired.Should().BeFalse();
        myLock.Release(); // shouldn't throw
        myLock.IsAcquired.Should().BeFalse();

        myLock.Acquire();
        myLock.IsAcquired.Should().BeTrue();

        myLock.Release();
        myLock.IsAcquired.Should().BeFalse();
    }


    [Fact]
    public async Task TestCancelWhileAcquiring()
    {
        var factory = new AtomicLock(_LockName);

        using var myLock = factory.Create();
        await myLock.AcquireAsync(default);

        using var otherLock = factory.Create();
        CancellationTokenSource cancelSource = new();
        var acquire = otherLock.AcquireAsync(cancelSource.Token);
        await Task.Delay(500); // wait for blocking

        otherLock.IsAcquired.Should().BeFalse();
        await cancelSource.CancelAsync();
        acquire.IsCanceled.Should().BeTrue();

        await myLock.ReleaseAsync();
        await Task.Delay(500); // wait maybe it wasn't canceled and acquired lock
        otherLock.IsAcquired.Should().BeFalse();
    }


    [Fact]
    public async Task TestNoDeadLock()
    {
        var factory = new AtomicLock(_LockName);

        await using var myLock = factory.Create();
        await myLock.AcquireAsync(default);

        {
            await using var otherLock = factory.Create();
            var task = otherLock.AcquireAsync(CancellationToken.None);
            await Task.Delay(500);
            await otherLock.ReleaseAsync();
            await task.Invoking(async task => await task).Should().ThrowAsync<SynchronizationLockException>();
        }

        {
            await using var otherLock = factory.Create();
            var task = otherLock.AcquireAsync(CancellationToken.None);
            await Task.Delay(500);
            await Task.WhenAll(
                Task.Run(async () => await myLock.ReleaseAsync()),
                Task.Run(async () => await otherLock.ReleaseAsync())
            );
            try
            {
                await task;
            }
            catch (SynchronizationLockException) { }
        }
    }


}
