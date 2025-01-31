// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;
using static AdHoc.ZooKeeper.Requests;
using static AdHoc.ZooKeeper.Responses;

namespace AdHoc.ZooKeeper;
public class ZooKeeperClient
    : IAsyncDisposable
{
    private sealed record Authentication(ReadOnlyMemory<byte> Scheme, ReadOnlyMemory<byte> Data);


    private TcpClient? _tcpClient;

    private CancellationTokenSource _disposeSource;

    private Task _receiveTask;
    private int _previousConnection;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<byte[]>> _pendingRequests;

    private readonly string _host;
    private readonly int _port;

    private byte[]? _sessionID;
    private byte[]? _sessionPassword;

    private const int SessionTimeout = 30000; // 30 seconds
    private const bool ReadOnly = false;

    private readonly SemaphoreSlim _lock = new(1, 1);

    public ZooKeeperClient(string host, int port)
    {
        _host = host;
        _port = port;
        _pendingRequests = new ConcurrentDictionary<int, TaskCompletionSource<byte[]>>();
        _receiveTask = Task.CompletedTask;
        _disposeSource = new CancellationTokenSource();
    }


    public async Task<IZooKeeperResponse> SendAsync(Memory<byte> request, CancellationToken cancellationToken)
    {
        SetConnection(request.Span, ref _previousConnection);

        var stream = await EnsureSessionAsync(cancellationToken);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            BitConverter.TryWriteBytes(request.Span.Slice(LengthSize), BinaryPrimitives.ReverseEndianness(++_previousConnection));
            await stream.WriteAsync(request, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            var bufferOwner = MemoryPool<byte>.Shared.Rent(LengthSize);
            try
            {
                var buffer = bufferOwner.Memory;
                if (await stream.ReadAsync(buffer.Slice(0, LengthSize), cancellationToken) != LengthSize)
                    throw new ZooKeeperException($"Invalid ZooKeeper response!");

                var responseLength = BinaryPrimitives.ReadInt32BigEndian(buffer.Span.Slice(0, LengthSize));
                if (responseLength > buffer.Length)
                {
                    bufferOwner.Dispose();
                    bufferOwner = MemoryPool<byte>.Shared.Rent(responseLength);
                }

                var response = buffer.Slice(0, responseLength);
                if (await stream.ReadAsync(response, cancellationToken) != responseLength)
                    throw new ZooKeeperException($"Invalid ZooKeeper response!");

                return new Response(bufferOwner, response);
            }
            catch
            {
                bufferOwner.Dispose();
                throw;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<Stream> EnsureSessionAsync(CancellationToken cancellationToken)
    {
        if (_tcpClient is not null && _tcpClient.Connected)
            return _tcpClient.GetStream();

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_tcpClient is not null && _tcpClient.Connected)
                return _tcpClient.GetStream();

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

                _sessionID = buffer.Span.Slice(8, SessionSize).ToArray();
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
            _lock.Release();
        }
    }


    public async Task PingAsync(CancellationToken cancellationToken)
    {
        using var pingOwner = MemoryPool<byte>.Shared.Rent(PingSize);
        var ping = pingOwner.Memory;
        CreatePing(ping.Span);
        using var response = await SendAsync(ping.Slice(0, PingSize), cancellationToken);

        int xid = BinaryPrimitives.ReadInt32BigEndian(response.Memory.Span);
        Console.WriteLine("xid: ");
        Console.WriteLine(xid);
        Console.WriteLine(xid.ToString("x"));

        long zxid = BinaryPrimitives.ReadInt64BigEndian(response.Memory.Span.Slice(4));
        Console.WriteLine("zxid: ");
        Console.WriteLine(zxid);
        Console.WriteLine(zxid.ToString("x"));

        int error = BinaryPrimitives.ReadInt32BigEndian(response.Memory.Span.Slice(12));
        Console.WriteLine("error: ");
        Console.WriteLine(error);
        Console.WriteLine(error.ToString("x"));

        // TODO Throw on error
    }


    public async ValueTask DisposeAsync()
    {
        if (_disposeSource != null)
        {
            await _disposeSource.CancelAsync();
            var receiveTask = _receiveTask;
            if (receiveTask is not null)
                await receiveTask;
            _tcpClient?.Dispose();
            _disposeSource.Dispose();
        }
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
