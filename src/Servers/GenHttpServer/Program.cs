using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using GenHTTP.Parser;
using GenHTTP.Types;
using Glyph11;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 5098;

var listener = new TcpListener(IPAddress.Loopback, port);
listener.Start();

Console.WriteLine($"GenHTTP server listening on http://localhost:{port}");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        var client = await listener.AcceptTcpClientAsync(cts.Token);
        _ = HandleClientAsync(client, cts.Token);
    }
}
catch (OperationCanceledException) { }

listener.Stop();
Console.WriteLine("Server stopped.");

static async Task HandleClientAsync(TcpClient client, CancellationToken ct)
{
    using (client)
    await using (var stream = client.GetStream())
    {
        var buffer = new byte[65536];
        var filled = 0;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(filled), ct);
                if (read == 0) break;
                filled += read;

                // Try to parse a complete request from the accumulated buffer
                while (filled > 0)
                {
                    var request = new Request();
                    var sequence = new ReadOnlySequence<byte>(buffer, 0, filled);

                    try
                    {
                        if (!RequestParser.TryParse(sequence, request, out var bytesRead))
                            break; // Need more data
                        
                        // Build and send response
                        var path = Encoding.ASCII.GetString(request.Raw.Path.Span);
                        var method = request.Method;
                        var responseBytes = BuildResponse(method, path);
                        await stream.WriteAsync(responseBytes, ct);

                        // Consume parsed bytes
                        if (bytesRead > 0 && bytesRead <= filled)
                        {
                            Buffer.BlockCopy(buffer, bytesRead, buffer, 0, filled - bytesRead);
                            filled -= bytesRead;
                        }
                        else
                        {
                            filled = 0;
                        }
                    }
                    catch (HttpParseException)
                    {
                        await stream.WriteAsync(MakeErrorResponse(400, "Bad Request"), ct);
                        return;
                    }
                }

                // Buffer full with no valid parse — reject
                if (filled >= buffer.Length)
                {
                    await stream.WriteAsync(MakeErrorResponse(431, "Request Header Fields Too Large"), ct);
                    return;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
    }
}

static byte[] BuildResponse(GenHTTP.Protocol.RequestMethod method, string path)
{
    var body = $"Hello from GenHTTP server\r\nMethod: {method}\r\nPath: {path}\r\n";
    return MakeResponse(200, "OK", body);
}

static byte[] MakeResponse(int status, string reason, string body)
{
    var bodyBytes = Encoding.UTF8.GetBytes(body);
    var header = $"HTTP/1.1 {status} {reason}\r\nContent-Type: text/plain\r\nContent-Length: {bodyBytes.Length}\r\nConnection: keep-alive\r\n\r\n";
    var headerBytes = Encoding.ASCII.GetBytes(header);

    var result = new byte[headerBytes.Length + bodyBytes.Length];
    Buffer.BlockCopy(headerBytes, 0, result, 0, headerBytes.Length);
    Buffer.BlockCopy(bodyBytes, 0, result, headerBytes.Length, bodyBytes.Length);
    return result;
}

static byte[] MakeErrorResponse(int status, string reason)
{
    return MakeResponse(status, reason, $"{status} {reason}\r\n");
}
