// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using static AdHoc.ZooKeeper.Abstractions.IZooKeeperWatcher;

namespace AdHoc.ZooKeeper.Abstractions;

public interface IZooKeeperWatcher
    : IAsyncDisposable
{
    // TODO its states in the events
    // https://github.com/apache/zookeeper/blob/a8eb7faa34e90c748f5f49f211a6dbad78c16f0b/zookeeper-server/src/main/java/org/apache/zookeeper/Watcher.java#L105
    public enum Types : int
    {
        Children = 1,
        Data = 2,
        Any = 3,
        Persistent = 4,
        PersistentRecursive = 5
    }

    Types Type { get; }


    public delegate ValueTask WatchAsync(IZooKeeperWatcher watcher, ZooKeeperEvent @event, CancellationToken cancellationToken);
    public delegate void Watch(IZooKeeperWatcher watcher, ZooKeeperEvent @event);
}


public static partial class Operations
{
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
    public static WatchAsync ToWatchAsync(this Watch watch) =>
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        (watcher, @event, cancellationToken) =>
        {
            watch(watcher, @event);
            return ValueTask.CompletedTask;
        };
}
