// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using AdHoc.ZooKeeper.Abstractions;
using static AdHoc.ZooKeeper.Operations;

namespace AdHoc.ZooKeeper;
public class ZooKeeperClient
    : IZooKeeper
{


    private readonly string _host;
    private readonly int _port;

    private byte[]? _sessionID;
    public string? SessionID => _sessionID is null ? null
        : BitConverter.ToString(_sessionID ?? []).Replace("-", "").ToLower();

    private byte[]? _sessionPassword;

    private const int SessionTimeout = 30000; // 30 seconds
    private const bool ReadOnly = false;


    private TcpClient? _tcpClient;
    private readonly SemaphoreSlim _lock = new(1, 1);



    private int _previousRequest;
    private Task _receiveTask;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<Response>> _pending;
    private readonly ConcurrentDictionary<int, Task> _receiving;
    private readonly CancellationTokenSource _disposeSource;


    public ZooKeeperClient(string host, int port)
    {
        _host = host;
        _port = port;
        _pending = new();
        _receiving = new();
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
        if (operation is not Ping)
        {
            (var result, wrote) = await WriteAsync(stream, async (stream, pending, cancellationToken) =>
            {
                var writer = new SafeRequestWriter(PipeWriter.Create(stream));
                operation.WriteRequest(
                    writer,
                    (writer, operation) =>
                    {
                        int connection;
                        do
                        {
                            connection = GetRequestID(operation, ref _previousRequest);
                            if (connection == Ping.RequestID)
                                break;
                        } while (!_pending.TryAdd(connection, pending));
                        writer.Write(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(connection)));
                    }
                );

                if (writer.IsPing)
                    return null;

                if (writer.IsCompleted)
                    throw ZooKeeperException.CreateInvalidRequestSize(writer.Length, writer.Size);

                return writer.RequestID;

            }, operation, cancellationToken);
            if (wrote)
                return result!;
        }

        // is ping
        Task<TResult>? ping = null;
        (var pingResult, wrote) = await WriteAsync(stream, async (stream, pending, cancellationToken) =>
        {
            if (_receiving.TryGetValue(Ping.RequestID, out var task))
            {
                if (operation is Ping)
                    ping = Task.Run(async () => await (Task<TResult>)task, cancellationToken);
                else
                {
                    if (!_pending.TryGetValue(Ping.RequestID, out var response))
                        throw new InvalidOperationException();
                    ping = Task.Run(async () => operation.ReadResponse(ToResponse(await response.Task)), cancellationToken);
                }
                return null;
            }

            Console.WriteLine("ping");
            _pending[Ping.RequestID] = pending;
            await stream.WriteAsync(Ping._Header, cancellationToken);
            await stream.FlushAsync(cancellationToken);
            return Ping.RequestID;
        }, operation, cancellationToken);
        return wrote ? pingResult! : await ping!;
    }

    private async Task<(TResult?, bool)> WriteAsync<TResult>(
        NetworkStream stream,
        Func<NetworkStream, TaskCompletionSource<Response>, CancellationToken, ValueTask<int?>> writeAsync,
        IZooKeeperOperation<TResult> operation,
        CancellationToken cancellationToken
    )
    {
        await _lock.WaitAsync(cancellationToken);
        TaskCompletionSource<Response> pending = new();
        int? connection = null;
        Task<TResult>? receiveTask = null;
        try
        {
            connection = await writeAsync(stream, pending, cancellationToken);
            if (connection is null)
            {
                _lock.Release();
                return (default, false);
            }

            receiveTask = ReceiveAsync(pending.Task, stream, operation, cancellationToken);
            _receiving[Ping.RequestID] = receiveTask;

            _lock.Release(); // release lock after writing and task management is done

            var result = await receiveTask;
            _receiving.TryRemove(KeyValuePair.Create<int, Task>(connection.Value, receiveTask));
            _pending.TryRemove(KeyValuePair.Create(connection.Value, pending));
            return (result, true);
        }
        catch (Exception ex)
        {
            _lock.Release();

            HandleException(ex, cancellationToken,
                (cancellationToken) =>
                {
                    if (connection is not null)
                    {
                        if (receiveTask is not null)
                            _receiving.TryRemove(KeyValuePair.Create<int, Task>(connection.Value, receiveTask));
                        if (_pending.TryRemove(KeyValuePair.Create(connection.Value, pending)))
                            pending.TrySetCanceled(cancellationToken);
                    }
                },
                (exception) =>
                {
                    if (connection is not null)
                    {
                        if (receiveTask is not null)
                            _receiving.TryRemove(KeyValuePair.Create<int, Task>(connection.Value, receiveTask));
                        if (_pending.TryRemove(KeyValuePair.Create(connection.Value, pending)))
                            pending.TrySetException(exception);
                    }
                }
            );
            throw;
        }
    }

    private async Task<TResult> ReceiveAsync<TResult>(
        Task<Response> pending,
        NetworkStream stream,
        IZooKeeperOperation<TResult> operation,
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
                _receiveTask = receiveTask = ReceiveResponsesAsync(stream, _disposeSource.Token);
            }

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
            await Task.WhenAny(receiveTask, pending);
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
        }

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        using var response = await pending;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
        return operation.ReadResponse(ToResponse(response));
    }

    private static ZooKeeperResponse ToResponse(Response response)
    {
        var span = response.Memory.Span;
        return new(
            response.RequestID,
            connectionID: BinaryPrimitives.ReadInt64BigEndian(span.Slice(RequestIDSize)),
            error: (ErrorCode)BinaryPrimitives.ReadInt32BigEndian(span.Slice(RequestIDSize + ConnectionIDSize)),
            data: span.Slice(RequestIDSize + ConnectionIDSize + ErrorSize)
        );
    }


    private async Task ReceiveResponsesAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        await Task.Yield();

        IMemoryOwner<byte>? bufferOwner = null;
        Memory<byte> buffer;
        try
        {
            while (!_pending.IsEmpty)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bufferOwner = MemoryPool<byte>.Shared.Rent(MinimalResponseLength);
                buffer = bufferOwner.Memory;
                if (await stream.ReadAsync(buffer.Slice(0, LengthSize), cancellationToken) != LengthSize)
                {
                    await DisconnectWithAsync(new ZooKeeperException($"Invalid ZooKeeper response!"), cancellationToken);
                    return;
                }

                var responseLength = BinaryPrimitives.ReadInt32BigEndian(buffer.Span.Slice(0, LengthSize));
                if (responseLength < MinimalResponseLength)
                {
                    await DisconnectWithAsync(new ZooKeeperException($"Invalid ZooKeeper response!"), cancellationToken);
                    return;
                }

                if (responseLength > buffer.Length)
                {
                    bufferOwner.Dispose();
                    bufferOwner = MemoryPool<byte>.Shared.Rent(responseLength);
                }

                var response = buffer.Slice(0, responseLength);
                if (await stream.ReadAsync(response, cancellationToken) != responseLength)
                {
                    var reader = PipeReader.Create(stream);
                    await DisconnectWithAsync(new ZooKeeperException($"Invalid ZooKeeper response!"), cancellationToken);
                    return;
                }

                var requestID = BinaryPrimitives.ReadInt32BigEndian(buffer.Span);
                if (_pending.TryRemove(requestID, out var request))
                {
                    if (!request.TrySetResult(new Response(bufferOwner, requestID, response)))
                        bufferOwner.Dispose();
                    bufferOwner = null;
                }
            }
        }
        catch (IOException ex) when (!stream.Socket.Connected)
        {
            await DisconnectWithAsync(new TimeoutException($"Session '0x{SessionID}' has timed out", ex), cancellationToken);
        }
        catch (Exception ex)
        {
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

                _sessionID = buffer.Span.Slice(8, SessionIDSize).ToArray();
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
            exception = new TimeoutException($"Session '0x{SessionID}' has timed out", innerException);

        @throw?.Invoke(exception);
        if (exception == innerException)
            ExceptionDispatchInfo.Throw(innerException); // keep stack trace
        else
            throw exception;
    }



    public async ValueTask DisposeAsync()
    {
        await _disposeSource.CancelAsync();
        var receiveTask = _receiveTask;
        if (receiveTask is not null)
            try { await receiveTask; } catch { }
        _tcpClient?.Dispose();
        _disposeSource.Dispose();
    }


    private readonly record struct PendingRequest(TaskCompletionSource<Response> ResponseSource, Task ResultTask);

    private readonly struct Response
        : IDisposable
    {
        private readonly IMemoryOwner<byte> _owner;

        public int RequestID { get; }
        public ReadOnlyMemory<byte> Memory { get; }

        public Response(IMemoryOwner<byte> owner, int requestID, ReadOnlyMemory<byte> memory)
        {
            _owner = owner;
            RequestID = requestID;
            Memory = memory;
        }

        public void Dispose() =>
            _owner.Dispose();
    }
}
