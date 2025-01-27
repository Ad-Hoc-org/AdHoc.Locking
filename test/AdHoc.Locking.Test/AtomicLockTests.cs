using FluentAssertions;

namespace AdHoc.Locking.Test;

public class AtomicLockTests
{


    private const string _LockName = "my-lock";
    private const string _OtherLockName = "other-lock";


    [Fact]
    public void TestProviderSingleton()
    {
        var provider = new AtomicLockProvider();
        var myLock = provider.GetAtomic(_LockName);
        myLock.Should().NotBeNull();
        myLock.Should().BeSameAs(provider.GetAtomic(_LockName));
        myLock.Should().NotBeSameAs(provider.GetAtomic(_OtherLockName));
    }


    [Fact]
    public void TestTryAcquire()
    {
        var myLock = new AtomicLock(_LockName);

        using var locking = myLock.Create();
        locking.TryAcquire().Should().BeTrue();
        locking.TryAcquire().Should().BeTrue();

        using var otherLocking = myLock.Create();
        otherLocking.TryAcquire().Should().BeFalse();

        locking.Release();
        otherLocking.TryAcquire().Should().BeTrue();
        locking.TryAcquire().Should().BeFalse();
    }


    [Fact]
    public async Task TestAcquire()
    {
        var myLock = new AtomicLock(_LockName);

        await using var locking = myLock.Create();
        locking.IsAcquired.Should().BeFalse();
        await locking.AcquireAsync(default);
        locking.IsAcquired.Should().BeTrue();

        // timeout only if doesn't work - faster tests
        await locking.Invoking(async l => await l.AcquireAsync(default))
            .Should().CompleteWithinAsync(TimeSpan.FromSeconds(1));

        await using var otherLocking = myLock.Create();
        var otherAcquire = Task.Run(async () =>
        {
            locking.IsAcquired.Should().BeTrue();
            await otherLocking.AcquireAsync(default);
            locking.IsAcquired.Should().BeFalse();
            otherLocking.IsAcquired.Should().BeTrue();
        });

        otherLocking.IsAcquired.Should().BeFalse();
        await Task.Delay(500); // wait for otherLock to arquire
        await locking.ReleaseAsync();
        await otherAcquire;
        otherLocking.IsAcquired.Should().BeTrue();
    }


    [Fact]
    public void TestRelease()
    {
        var myLock = new AtomicLock(_LockName);

        using var locking = myLock.Create();
        locking.IsAcquired.Should().BeFalse();
        locking.Release(); // shouldn't throw
        locking.IsAcquired.Should().BeFalse();

        locking.Acquire();
        locking.IsAcquired.Should().BeTrue();

        locking.Release();
        locking.IsAcquired.Should().BeFalse();
    }


    [Fact]
    public async Task TestCancelWhileAcquiring()
    {
        var myLock = new AtomicLock(_LockName);

        using var locking = myLock.Create();
        await locking.AcquireAsync(default);

        using var otherLocking = myLock.Create();
        CancellationTokenSource cancelSource = new();
        var acquire = otherLocking.AcquireAsync(cancelSource.Token);
        await Task.Delay(500); // wait for blocking

        otherLocking.IsAcquired.Should().BeFalse();
        await cancelSource.CancelAsync();
        acquire.IsCanceled.Should().BeTrue();

        await locking.ReleaseAsync();
        await Task.Delay(500); // wait maybe it wasn't canceled and acquired lock
        otherLocking.IsAcquired.Should().BeFalse();
    }


    [Fact]
    public async Task TestNoDeadLock()
    {
        var myLock = new AtomicLock(_LockName);

        await using var locking = myLock.Create();
        await locking.AcquireAsync(default);

        {
            await using var otherLocking = myLock.Create();
            var task = otherLocking.AcquireAsync(CancellationToken.None);
            await Task.Delay(500);
            await otherLocking.ReleaseAsync();
            await task.Invoking(async task => await task).Should().ThrowAsync<SynchronizationLockException>();
        }

        {
            await using var otherLocking = myLock.Create();
            var task = otherLocking.AcquireAsync(CancellationToken.None);
            await Task.Delay(500);
            await Task.WhenAll(
                Task.Run(async () => await locking.ReleaseAsync()),
                Task.Run(async () => await otherLocking.ReleaseAsync())
            );
            try
            {
                await task;
            }
            catch (SynchronizationLockException) { }
        }
    }


}
