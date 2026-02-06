using System.Net;
using System.Net.Sockets;

namespace Glyph11.Probe.Client;

public sealed class RawTcpClient : IAsyncDisposable
{
    private Socket? _socket;
    private readonly TimeSpan _connectTimeout;
    private readonly TimeSpan _readTimeout;

    public RawTcpClient(TimeSpan connectTimeout, TimeSpan readTimeout)
    {
        _connectTimeout = connectTimeout;
        _readTimeout = readTimeout;
    }

    public async Task<ConnectionState> ConnectAsync(string host, int port)
    {
        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            using var cts = new CancellationTokenSource(_connectTimeout);
            var addresses = await Dns.GetHostAddressesAsync(host, cts.Token);
            if (addresses.Length == 0)
                return ConnectionState.Error;

            await _socket.ConnectAsync(new IPEndPoint(addresses[0], port), cts.Token);
            return ConnectionState.Open;
        }
        catch (OperationCanceledException)
        {
            return ConnectionState.TimedOut;
        }
        catch
        {
            return ConnectionState.Error;
        }
    }

    public async Task SendAsync(ReadOnlyMemory<byte> data)
    {
        if (_socket is null)
            throw new InvalidOperationException("Not connected.");

        var sent = 0;
        while (sent < data.Length)
        {
            sent += await _socket.SendAsync(data[sent..], SocketFlags.None);
        }
    }

    public async Task<(byte[] Data, int Length, ConnectionState State)> ReadResponseAsync()
    {
        if (_socket is null)
            return ([], 0, ConnectionState.Error);

        var buffer = new byte[65536];
        var totalRead = 0;

        using var cts = new CancellationTokenSource(_readTimeout);

        try
        {
            while (totalRead < buffer.Length)
            {
                var read = await _socket.ReceiveAsync(
                    buffer.AsMemory(totalRead),
                    SocketFlags.None,
                    cts.Token);

                if (read == 0)
                    return (buffer, totalRead, ConnectionState.ClosedByServer);

                totalRead += read;

                // Check if we've received the end of headers
                if (ContainsHeaderTerminator(buffer.AsSpan(0, totalRead)))
                    break;
            }

            return (buffer, totalRead, ConnectionState.Open);
        }
        catch (OperationCanceledException)
        {
            return (buffer, totalRead, ConnectionState.TimedOut);
        }
        catch (SocketException)
        {
            return (buffer, totalRead, ConnectionState.ClosedByServer);
        }
        catch
        {
            return (buffer, totalRead, ConnectionState.Error);
        }
    }

    public ConnectionState CheckConnectionState()
    {
        if (_socket is null || !_socket.Connected)
            return ConnectionState.ClosedByServer;

        try
        {
            // Poll for readability with zero timeout — if readable and Receive would return 0, peer closed
            if (_socket.Poll(0, SelectMode.SelectRead))
            {
                var buf = new byte[1];
                var read = _socket.Receive(buf, SocketFlags.Peek);
                return read == 0 ? ConnectionState.ClosedByServer : ConnectionState.Open;
            }

            return ConnectionState.Open;
        }
        catch
        {
            return ConnectionState.ClosedByServer;
        }
    }

    private static bool ContainsHeaderTerminator(ReadOnlySpan<byte> data)
    {
        // Look for \r\n\r\n
        ReadOnlySpan<byte> terminator = [0x0D, 0x0A, 0x0D, 0x0A];
        return data.IndexOf(terminator) >= 0;
    }

    public async ValueTask DisposeAsync()
    {
        if (_socket is not null)
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                // Ignore — socket may already be closed
            }

            _socket.Dispose();
            _socket = null;
        }

        await ValueTask.CompletedTask;
    }
}
