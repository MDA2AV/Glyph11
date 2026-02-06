using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Glyph11;
using Glyph11.Parser.Hardened;
using Glyph11.Protocol;
using Glyph11.Validation;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 5098;

var listener = new TcpListener(IPAddress.Loopback, port);
listener.Start();

Console.WriteLine($"GlyphServer listening on http://localhost:{port}");

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
        var limits = ParserLimits.Default;
        using var request = new BinaryRequest();

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(filled), ct);
                if (read == 0) break;
                filled += read;

                while (filled > 0)
                {
                    var sequence = new ReadOnlySequence<byte>(buffer, 0, filled);

                    try
                    {
                        if (!HardenedParser.TryExtractFullHeader(ref sequence, request, in limits, out var bytesRead))
                            break; // Need more data

                        // Post-parse semantic validation
                        if (RequestSemantics.HasTransferEncodingWithContentLength(request) ||
                            RequestSemantics.HasConflictingContentLength(request) ||
                            RequestSemantics.HasConflictingCommaSeparatedContentLength(request) ||
                            RequestSemantics.HasInvalidContentLengthFormat(request) ||
                            RequestSemantics.HasContentLengthWithLeadingZeros(request) ||
                            RequestSemantics.HasInvalidHostHeaderCount(request) ||
                            RequestSemantics.HasInvalidTransferEncoding(request) ||
                            RequestSemantics.HasDotSegments(request) ||
                            RequestSemantics.HasFragmentInRequestTarget(request) ||
                            RequestSemantics.HasBackslashInPath(request) ||
                            RequestSemantics.HasDoubleEncoding(request) ||
                            RequestSemantics.HasEncodedNullByte(request) ||
                            RequestSemantics.HasOverlongUtf8(request))
                        {
                            await stream.WriteAsync(MakeErrorResponse(400, "Bad Request"), ct);
                            return;
                        }

                        var method = Encoding.ASCII.GetString(request.Method.Span);
                        var path = Encoding.ASCII.GetString(request.Path.Span);
                        var responseBytes = BuildResponse(method, path);
                        await stream.WriteAsync(responseBytes, ct);

                        // Consume parsed bytes and reset for keep-alive
                        if (bytesRead > 0 && bytesRead <= filled)
                        {
                            Buffer.BlockCopy(buffer, bytesRead, buffer, 0, filled - bytesRead);
                            filled -= bytesRead;
                        }
                        else
                        {
                            filled = 0;
                        }

                        request.Clear();
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

static byte[] BuildResponse(string method, string path)
{
    var body = $"Hello from GlyphServer\r\nMethod: {method}\r\nPath: {path}\r\n";
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
