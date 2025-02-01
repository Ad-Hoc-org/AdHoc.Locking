// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net.Sockets;
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
    private readonly SemaphoreSlim _connectionLock = new(1, 1);



    private int _previousRequest;
    private Task _receiveTask;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<IZooKeeperResponse>> _pendingRequests;
    private readonly CancellationTokenSource _disposeSource;


    public ZooKeeperClient(string host, int port)
    {
        _host = host;
        _port = port;
        _pendingRequests = new();
        _receiveTask = Task.CompletedTask;
        _disposeSource = new();
    }


    private async Task ReceiveResponsesAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        await Task.Yield();

        await Task.Delay(5000);

        IMemoryOwner<byte>? bufferOwner = null;
        Memory<byte> buffer;
        try
        {
            while (!_pendingRequests.IsEmpty)
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
                    await DisconnectWithAsync(new ZooKeeperException($"Invalid ZooKeeper response!"), cancellationToken);
                    return;
                }

                var requestID = BinaryPrimitives.ReadInt32BigEndian(buffer.Span);
                if (_pendingRequests.TryRemove(requestID, out var request))
                {
                    if (!request.TrySetResult(new Response(bufferOwner, response)))
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
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            _tcpClient?.Close();
            while (!_pendingRequests.IsEmpty)
                if (_pendingRequests.TryRemove(_pendingRequests.Keys.First(), out var request))
                    request.TrySetException(exception);
        }
        finally
        {
            _connectionLock.Release();
        }
    }


    public async Task<IZooKeeperResponse> SendAsync(Memory<byte> request, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposeSource.Token.IsCancellationRequested, this);

        var taskSource = new TaskCompletionSource<IZooKeeperResponse>();
        int connection;
        do
        {
            connection = SetConnection(request.Span, ref _previousRequest);
            if (connection == PingConnectionID && _pendingRequests.TryGetValue(connection, out var ping))
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
                return await Task.Run(async () => await ping.Task, cancellationToken);
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
        } while (!_pendingRequests.TryAdd(connection, taskSource));

        NetworkStream? stream = null;
        try
        {
            stream = await EnsureSessionAsync(cancellationToken);

            BitConverter.TryWriteBytes(request.Span.Slice(LengthSize), BinaryPrimitives.ReverseEndianness(++_previousRequest));
            new PipeWriter(stream).Write(request.Span);
            await stream.WriteAsync(request, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            Task receiveTask;
            while (!taskSource.Task.IsCompleted)
            {
                cancellationToken.ThrowIfCancellationRequested();

                receiveTask = _receiveTask;
                if (receiveTask.IsCompleted)
                {
                    ObjectDisposedException.ThrowIf(receiveTask.IsCanceled, true);
                    _receiveTask = receiveTask = ReceiveResponsesAsync(stream, _disposeSource.Token);
                }

                await Task.WhenAny(receiveTask, taskSource.Task);
            }

            return await taskSource.Task;
        }
        catch (Exception innerException)
        {
            if (innerException is TaskCanceledException canceledException && canceledException.CancellationToken == cancellationToken)
            {
                if (_pendingRequests.TryRemove(KeyValuePair.Create(connection, taskSource)))
                    taskSource.TrySetCanceled(cancellationToken);
                throw;
            }

            Exception exception = innerException;
            if (innerException is IOException && stream is not null && !stream.Socket.Connected)
                exception = new TimeoutException($"Session '0x{SessionID}' has timed out", innerException);

            if (_pendingRequests.TryRemove(KeyValuePair.Create(connection, taskSource)))
                taskSource.TrySetException(innerException);
            if (exception == innerException)
                throw; // keep stack trace
            else
                throw exception;
        }
    }

    private async Task<NetworkStream> EnsureSessionAsync(CancellationToken cancellationToken)
    {
        if (_tcpClient is not null && _tcpClient.Connected)
            return _tcpClient.GetStream();

        await _connectionLock.WaitAsync(cancellationToken);
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
        finally
        {
            _connectionLock.Release();
        }
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

    private sealed class Response
        : IZooKeeperResponse
    {
        private readonly IMemoryOwner<byte> _memoryOwner;

        public ReadOnlyMemory<byte> Memory { get; }

        internal Response(IMemoryOwner<byte> memoryOwner, Memory<byte> memory)
        {
            _memoryOwner = memoryOwner;
            Memory = memory;
        }

        public void Dispose() =>
            _memoryOwner.Dispose();
    }
}
