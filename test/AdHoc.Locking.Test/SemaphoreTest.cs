using FluentAssertions;

namespace AdHoc.Locking.Test;

public class SemaphoreTests
{


    private const string _LockName = "my-lock";
    private const string _OtherLockName = "other-lock";


    [Fact]
    public void TestFactorySingleton()
    {
        var provider = new SemaphoreProvider();
        var factory = provider.GetLock(_LockName);
        factory.Should().NotBeNull();
        factory.Should().BeSameAs(provider.GetLock(_LockName));
        factory.Should().NotBeSameAs(provider.GetLock(_OtherLockName));
    }


    [Fact]
    public void TestTryAcquire()
    {
        var factory = new Semaphore(_LockName, 2);

        using var myLock = factory.Create();
        myLock.TryAcquire().Should().BeTrue();
        myLock.TryAcquire().Should().BeTrue();

        using var otherLock = factory.Create();
        otherLock.TryAcquire().Should().BeTrue();
        otherLock.TryAcquire(2).Should().BeFalse();

        myLock.Release();
        otherLock.TryAcquire(2).Should().BeTrue();
        myLock.TryAcquire().Should().BeFalse();
    }


    [Fact]
    public async Task TestAcquire()
    {
        var factory = new Semaphore(_LockName, 2);

        await using var myLock = factory.Create();
        myLock.AcquiredCount.Should().Be(0);
        await myLock.AcquireAsync(2, default);
        myLock.AcquiredCount.Should().Be(2);

        // timeout only if doesn't work - faster tests
        await myLock.Invoking(async l => await l.AcquireAsync(default))
            .Should().CompleteWithinAsync(TimeSpan.FromSeconds(1));

        await using var otherLock = factory.Create();
        var fullAcquiring = Task.Run(async () =>
        {
            await otherLock.AcquireAsync(2, default);
            myLock.AcquiredCount.Should().Be(0);
            otherLock.AcquiredCount.Should().Be(2);
        });
        var oneAcquiring = Task.Run(async () =>
        {
            await otherLock.AcquireAsync(1, default);
            myLock.AcquiredCount.Should().BeLessThan(2);
            otherLock.AcquiredCount.Should().BeGreaterThan(0);
        });

        await Task.Delay(500); // wait for otherLock to arquire
        otherLock.AcquiredCount.Should().Be(0);
        await myLock.ReleaseAsync(1);
        await oneAcquiring;
        fullAcquiring.IsCompleted.Should().BeFalse();
        await myLock.ReleaseAsync();
        await fullAcquiring;
        otherLock.AcquiredCount.Should().Be(2);
    }


    [Fact]
    public void TestRelease()
    {
        var factory = new Semaphore(_LockName, 2);

        using var myLock = factory.Create();
        myLock.AcquiredCount.Should().Be(0);
        myLock.Release(); // shouldn't throw
        myLock.AcquiredCount.Should().Be(0);

        myLock.Acquire();
        myLock.AcquiredCount.Should().Be(1);

        myLock.Release();
        myLock.AcquiredCount.Should().Be(0);

        myLock.Acquire(2);
        myLock.AcquiredCount.Should().Be(2);
        myLock.Release(2);
        myLock.AcquiredCount.Should().Be(2);
        myLock.Release(1);
        myLock.AcquiredCount.Should().Be(1);
        myLock.Release(2);
        myLock.AcquiredCount.Should().Be(1);
        myLock.Release();
        myLock.AcquiredCount.Should().Be(0);
    }


    [Fact]
    public async Task TestCancelWhileAcquiring()
    {
        var factory = new Semaphore(_LockName, 2);

        using var myLock = factory.Create();
        await myLock.AcquireAsync(2, default);

        using var otherLock = factory.Create();
        CancellationTokenSource cancelSource = new();
        var fullAcquire = otherLock.AcquireAsync(2, cancelSource.Token);
        var acquire = otherLock.AcquireAsync(1, cancelSource.Token);
        await Task.Delay(500); // wait for blocking

        otherLock.AcquiredCount.Should().Be(0);
        await myLock.ReleaseAsync(1);
        await cancelSource.CancelAsync();
        await acquire;
        fullAcquire.IsCanceled.Should().BeTrue();

        await myLock.ReleaseAsync();
        await Task.Delay(500); // wait maybe it wasn't canceled and acquired lock
        otherLock.AcquiredCount.Should().Be(1);
    }


}
