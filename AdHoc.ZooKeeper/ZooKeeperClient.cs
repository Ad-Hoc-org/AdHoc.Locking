// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using AdHoc.ZooKeeper.Abstractions;
using static AdHoc.ZooKeeper.Abstractions.IZooKeeperWatcher;
using static AdHoc.ZooKeeper.Abstractions.Operations;

namespace AdHoc.ZooKeeper;
public class ZooKeeperClient
    : IZooKeeper
{


    private readonly string _host;
    private readonly int _port;

    private byte[]? _session;
    public string? Session => _session is null ? null
        : BitConverter.ToString(_session ?? []).Replace("-", "").ToLower();

    private byte[]? _sessionPassword;

    private const int SessionTimeout = 30000; // 30 seconds
    private const bool ReadOnly = false;


    private TcpClient? _tcpClient;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly ZooKeeperPath Root = ZooKeeperPath.Root;

    private int _previousRequest;
    private Task _receiveTask;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Watcher, WatchAsync>> _watchers;

    private readonly ConcurrentDictionary<int, TaskCompletionSource<Response>> _pending;
    private readonly ConcurrentDictionary<int, Task> _receiving;

    private readonly CancellationTokenSource _disposeSource;


    public ZooKeeperClient(string host, int port)
    {
        _host = host;
        _port = port;
        _pending = new();
        _receiving = new();
        _watchers = new();
        _receiveTask = Task.CompletedTask;
        _disposeSource = new();
    }


    public async Task<TResult> ExecuteAsync<TResult>(IZooKeeperOperation<TResult> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ObjectDisposedException.ThrowIf(_disposeSource.Token.IsCancellationRequested, this);

        var stream = await EnsureSessionAsync(cancellationToken);
        return await SendAsync(stream, operation, cancellationToken);
    }

    private async Task<TResult> SendAsync<TResult>(
        NetworkStream stream,
        IZooKeeperOperation<TResult> operation,
        CancellationToken cancellationToken
    )
    {
        bool wrote;
        if (operation is not PingOperation)
        {
            (var result, wrote) = await WriteAsync(stream, async (stream, pending, cancellationToken) =>
            {
                var pipeWriter = PipeWriter.Create(stream);
                var writer = new SafeRequestWriter(pipeWriter);
                bool hasRequest = false;
                Watcher? watcher = null;
                operation.WriteRequest(new(
                    Root,
                    writer,
                    (operation) =>
                    {
                        int request;
                        do
                        {
                            request = GetRequest(operation, ref _previousRequest);
                            if (request == PingOperation.Request)
                                break;
                        } while (!_pending.TryAdd(request, pending));
                        hasRequest = true;
                        return request;
                    },
                    (IEnumerable<ZooKeeperPath> paths, Types type, WatchAsync watch) =>
                        watcher = RegisterWatcher(paths, type, watch)
                ));

                if (writer.IsPing)
                    return default;

                if (writer.IsCompleted)
                    throw ZooKeeperException.CreateInvalidRequestSize(writer.Length, writer.Size);

                if (!hasRequest)
                    throw new ZooKeeperException("Request identifier has to be requested from context!");

                await pipeWriter.FlushAsync(cancellationToken);
                return (writer.Request, watcher);

            }, operation, cancellationToken);
            if (wrote)
                return result!;
        }

        // is ping
        Task<TResult>? ping = null;
        (var pingResult, wrote) = await WriteAsync(stream, async (stream, pending, cancellationToken) =>
        {
            if (_receiving.TryGetValue(PingOperation.Request, out var task))
            {
                if (operation is PingOperation)
                    ping = Task.Run(async () => await (Task<TResult>)task, cancellationToken);
                else
                {
                    if (!_pending.TryGetValue(PingOperation.Request, out var response))
                        throw new InvalidOperationException();
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
                    ping = Task.Run(async () => operation.ReadResponse(ToResponse(await response.Task), null), cancellationToken);
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
                }
                return default;
            }

            _pending[PingOperation.Request] = pending;
            await stream.WriteAsync(PingOperation._Header, cancellationToken);
            await stream.FlushAsync(cancellationToken);
            return (PingOperation.Request, null);
        }, operation, cancellationToken);
        return wrote ? pingResult! : await ping!;
    }


    private Watcher RegisterWatcher(IEnumerable<ZooKeeperPath> paths, Types type, WatchAsync watch)
    {
        var watcherPaths = paths.Select(p => p.Absolute().Value).ToImmutableHashSet();
        var watcher = new Watcher(this, watcherPaths, type);
        foreach (var path in watcherPaths)
            _watchers.AddOrUpdate(path,
                _ =>
                {
                    ConcurrentDictionary<Watcher, WatchAsync> watchers = new();
                    watchers[watcher] = watch;
                    return watchers;
                },
                (_, watchers) =>
                {
                    watchers.TryAdd(watcher, watch);
                    return watchers;
                }
            );
        return watcher;
    }


    private async Task<(TResult?, bool)> WriteAsync<TResult>(
        NetworkStream stream,
        Func<NetworkStream, TaskCompletionSource<Response>, CancellationToken, ValueTask<(int?, Watcher?)>> writeAsync,
        IZooKeeperOperation<TResult> operation,
        CancellationToken cancellationToken
    )
    {
        await _lock.WaitAsync(cancellationToken);
        bool released = false;
        TaskCompletionSource<Response> pending = new();
        int? request = null;
        Task<TResult>? receiveTask = null;
        try
        {
            (request, var watcher) = await writeAsync(stream, pending, cancellationToken);
            if (request is null)
                return (default, false);

            receiveTask = ReceiveAsync(pending.Task, stream, operation, watcher, cancellationToken);
            _receiving[PingOperation.Request] = receiveTask;

            // release lock after writing and task management is done
            _lock.Release();
            released = true;

            var result = await receiveTask;
            _receiving.TryRemove(KeyValuePair.Create<int, Task>(request.Value, receiveTask));
            _pending.TryRemove(KeyValuePair.Create(request.Value, pending));
            return (result, true);
        }
        catch (Exception ex)
        {
            HandleException(ex, cancellationToken,
                (cancellationToken) =>
                {
                    if (request is not null)
                    {
                        if (receiveTask is not null)
                            _receiving.TryRemove(KeyValuePair.Create<int, Task>(request.Value, receiveTask));
                        if (_pending.TryRemove(KeyValuePair.Create(request.Value, pending)))
                            pending.TrySetCanceled(cancellationToken);
                    }
                },
                (exception) =>
                {
                    if (request is not null)
                    {
                        if (receiveTask is not null)
                            _receiving.TryRemove(KeyValuePair.Create<int, Task>(request.Value, receiveTask));
                        if (_pending.TryRemove(KeyValuePair.Create(request.Value, pending)))
                            pending.TrySetException(exception);
                    }
                }
            );
            throw;
        }
        finally
        {
            if (!released)
                _lock.Release();
        }
    }

    private async Task<TResult> ReceiveAsync<TResult>(
        Task<Response> pending,
        NetworkStream stream,
        IZooKeeperOperation<TResult> operation,
        Watcher? watcher,
        CancellationToken cancellationToken
    )
    {
        Task receiveTask;
        while (!pending.IsCompleted)
        {
            cancellationToken.ThrowIfCancellationRequested();

            receiveTask = _receiveTask;
            if (receiveTask.IsCompleted)
            {
                ObjectDisposedException.ThrowIf(receiveTask.IsCanceled, true);
                _receiveTask = receiveTask = ReceiveResponsesAsync(stream);
            }

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
            await Task.WhenAny(receiveTask, pending);
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
        }

        return operation.ReadResponse(
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
            ToResponse(await pending),
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
            watcher
        );
    }

    private ZooKeeperResponse ToResponse(Response response)
    {
        var span = response.Memory.Span;
        return new(
            Root,
            response.Request,
            transaction: BinaryPrimitives.ReadInt64BigEndian(span.Slice(RequestSize)),
            status: (ZooKeeperStatus)BinaryPrimitives.ReadInt32BigEndian(span.Slice(RequestSize + TransactionSize)),
            data: span.Slice(RequestSize + TransactionSize + StatusSize)
        );
    }


    private async Task ReceiveResponsesAsync(NetworkStream stream)
    {
        CancellationToken cancellationToken = _disposeSource.Token;
        await Task.Yield();

        IMemoryOwner<byte>? bufferOwner = null;
        Memory<byte> buffer;
        try
        {
            while (!_pending.IsEmpty || !_watchers.IsEmpty)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bufferOwner = MemoryPool<byte>.Shared.Rent(MinimalResponseLength);
                buffer = bufferOwner.Memory;
                var x = await stream.ReadAsync(buffer.Slice(0, LengthSize), cancellationToken);
                if (x != LengthSize)
                {
                    await DisconnectWithAsync(new ZooKeeperException($"Invalid ZooKeeper response!"), cancellationToken);
                    return;
                }

                var responseLength = ReadInt32(buffer.Span.Slice(0, LengthSize));
                if (responseLength < MinimalResponseLength)
                {
                    await DisconnectWithAsync(new ZooKeeperException($"Invalid ZooKeeper response!"), cancellationToken);
                    return;
                }

                if (responseLength > buffer.Length)
                {
                    bufferOwner.Dispose();
                    bufferOwner = MemoryPool<byte>.Shared.Rent(responseLength);
                    buffer = bufferOwner.Memory;
                }

                var response = buffer.Slice(0, responseLength);
                if (await stream.ReadAsync(response, cancellationToken) != responseLength)
                {
                    var reader = PipeReader.Create(stream);
                    await DisconnectWithAsync(new ZooKeeperException($"Invalid ZooKeeper response!"), cancellationToken);
                    return;
                }

                var requestIdentifier = ReadInt32(response.Span);
                if (_pending.TryRemove(requestIdentifier, out var request))
                {
                    if (!request.TrySetResult(new Response(bufferOwner, requestIdentifier, response)))
                        bufferOwner.Dispose();
                    bufferOwner = null;
                }
                else if (requestIdentifier == ZooKeeperEvent.NoRequest)
                {
                    try
                    {
                        var @event = ZooKeeperEvent.Read(response.Span, out _);
                        var path = @event.Path.Value;
                        if (_watchers.TryGetValue(path, out var watchers))
                        {
                            foreach (var watchPair in watchers)
                                try
                                {
                                    watchers.TryRemove(watchPair);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                                    // run in background
                                    watchPair.Value(watchPair.Key, @event, _disposeSource.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                                }
                                catch
                                {
                                    // TODO log handler
                                }


                            if (watchers.IsEmpty && _watchers.TryRemove(KeyValuePair.Create(path, watchers)))
                            {
                                // readd after watchers was added before removing
                                if (!watchers.IsEmpty)
                                    _watchers.AddOrUpdate(path, watchers, (_, newWatches) =>
                                    {
                                        foreach (var watcher in watchers)
                                            newWatches.TryAdd(watcher.Key, watcher.Value);
                                        return newWatches;
                                    });
                            }
                        }
                    }
                    // failed read event
                    catch (Exception ex)
                    {
                        await DisconnectWithAsync(new ZooKeeperException($"Invalid ZooKeeper event!", ex), cancellationToken);
                    }
                }
            }
        }
        catch (Exception ex) when (!stream.Socket.Connected)
        {
            // TODO if session is null never connected
            await DisconnectWithAsync(ZooKeeperException.CreateSessionExpired(Session!, ex), cancellationToken);
        }
        catch (Exception ex)
        {
            // TODO if canceled by dispose close session graceful
            await DisconnectWithAsync(ex, cancellationToken);
        }
        finally
        {
            bufferOwner?.Dispose();
        }
    }

    private async Task DisconnectWithAsync(Exception exception, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _tcpClient?.Close();
            while (!_pending.IsEmpty)
                if (_pending.TryRemove(_pending.Keys.First(), out var request))
                    request.TrySetException(exception);
        }
        finally
        {
            _lock.Release();
        }
    }


    private async Task<NetworkStream> EnsureSessionAsync(CancellationToken cancellationToken)
    {
        if (_tcpClient is not null && _tcpClient.Connected)
            return _tcpClient.GetStream();

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_tcpClient is not null && _tcpClient.Connected)
                return _tcpClient.GetStream();

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
            // clean up lost requests before reconnecting
            await _receiveTask; // 
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks

            _tcpClient ??= new TcpClient();
            await _tcpClient.ConnectAsync(_host, _port)
                .ConfigureAwait(true);
            var stream = _tcpClient.GetStream();

            var bufferOwner = MemoryPool<byte>.Shared.Rent(DefaultSessionResponseSize);
            try
            {
                var buffer = bufferOwner.Memory;
                CreateStartSession(buffer.Span, SessionTimeout, false);
                await stream.WriteAsync(buffer.Slice(0, StartSessionSize), cancellationToken);
                await stream.FlushAsync(cancellationToken);

                if (await stream.ReadAsync(buffer.Slice(0, LengthSize), cancellationToken) != LengthSize)
                    throw new ZooKeeperException($"Invalid ZooKeeper response!");

                var responseLength = BinaryPrimitives.ReadInt32BigEndian(buffer.Span.Slice(0, LengthSize));
                if (responseLength <= DefaultSessionResponseSize - DefaultPasswordSize)
                    throw new ZooKeeperException($"Invalid ZooKeeper response!");
                if (responseLength > buffer.Length)
                {
                    bufferOwner.Dispose();
                    bufferOwner = MemoryPool<byte>.Shared.Rent(responseLength);
                }

                if (await stream.ReadAsync(buffer.Slice(0, responseLength), cancellationToken) != responseLength)
                    throw new ZooKeeperException($"Invalid ZooKeeper response!");

                _session = buffer.Span.Slice(8, SessionSize).ToArray();
                _sessionPassword = new byte[BinaryPrimitives.ReadInt32BigEndian(buffer.Span.Slice(16, LengthSize))];
                buffer.Slice(20, _sessionPassword.Length).CopyTo(_sessionPassword);

                return stream;
            }
            finally
            {
                bufferOwner.Dispose();
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, cancellationToken);
            throw;
        }

        finally
        {
            _lock.Release();
        }
    }


    [DoesNotReturn]
    private void HandleException(
        Exception innerException,
        CancellationToken cancellationToken,
        Action<CancellationToken>? cancel = null,
        Action<Exception>? @throw = null
    )
    {
        if (innerException is OperationCanceledException canceledException && canceledException.CancellationToken == cancellationToken)
        {
            cancel?.Invoke(cancellationToken);
            ExceptionDispatchInfo.Throw(innerException); // keep stack trace
        }

        Exception exception = innerException;
        if (innerException is SocketException)
            // TODO if session is null couldn't connect
            exception = ZooKeeperException.CreateSessionExpired(Session!, innerException);

        @throw?.Invoke(exception);
        if (exception == innerException)
            ExceptionDispatchInfo.Throw(innerException); // keep stack trace
        else
            throw exception;
    }


    // TODO dispose watchers
    public async ValueTask DisposeAsync()
    {
        await _disposeSource.CancelAsync();
        var receiveTask = _receiveTask;
        if (receiveTask is not null)
            try { await receiveTask; } catch { }
        _tcpClient?.Dispose();
        _disposeSource.Dispose();
    }



    private class Watcher
        : IZooKeeperWatcher
    {
        private readonly ZooKeeperClient _client;
        private readonly ImmutableHashSet<string> _paths;

        public Watcher(ZooKeeperClient client, ImmutableHashSet<string> paths, Types type)
        {
            _client = client;
            _paths = paths;
            Type = type;
        }

        public Types Type { get; }

        public ValueTask DisposeAsync()
        {
            foreach (var path in _paths)
                if (_client._watchers.TryGetValue(path, out var watchers))
                    watchers.TryRemove(this, out _);
            return ValueTask.CompletedTask;
        }
    }


    private readonly record struct PendingRequest(TaskCompletionSource<Response> ResponseSource, Task ResultTask);

    private readonly struct Response
        : IDisposable
    {
        private readonly IMemoryOwner<byte> _owner;

        public int Request { get; }
        public ReadOnlyMemory<byte> Memory { get; }

        public Response(IMemoryOwner<byte> owner, int request, ReadOnlyMemory<byte> memory)
        {
            _owner = owner;
            Request = request;
            Memory = memory;
        }

        public void Dispose() =>
            _owner.Dispose();
    }
}
