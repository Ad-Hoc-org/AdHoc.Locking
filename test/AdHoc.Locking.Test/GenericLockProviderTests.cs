// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using AdHoc.Locking.Abstractions;
using FluentAssertions;

namespace AdHoc.Locking.Test;
public class GenericLockProviderTests
{

    [Fact]
    public void TestEmpty()
    {
        FluentActions.Invoking(() => new GenericLockProvider().GetLock<ILock>("empty"))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TestConfigured()
    {
        new GenericLockProvider()
            .Add(_ => true, name => new AtomicLock(name))
            .GetLock<ILock>("atomic")
            .Should().BeOfType(typeof(AtomicLock));
    }

}
