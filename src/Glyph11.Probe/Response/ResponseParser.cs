using System.Text;

namespace Glyph11.Probe.Response;

public static class ResponseParser
{
    public static HttpResponse? TryParse(ReadOnlySpan<byte> data, int length)
    {
        if (length == 0)
            return null;

        var text = Encoding.ASCII.GetString(data[..length]);

        // Find end of status line
        var lineEnd = text.IndexOf('\n');
        if (lineEnd < 0)
            return null;

        var statusLine = text[..lineEnd].TrimEnd('\r');

        // Parse "HTTP/x.x SSS Reason"
        var firstSpace = statusLine.IndexOf(' ');
        if (firstSpace < 0)
            return null;

        var httpVersion = statusLine[..firstSpace];

        var rest = statusLine[(firstSpace + 1)..];
        var secondSpace = rest.IndexOf(' ');

        string statusCodeStr;
        string reasonPhrase;

        if (secondSpace >= 0)
        {
            statusCodeStr = rest[..secondSpace];
            reasonPhrase = rest[(secondSpace + 1)..];
        }
        else
        {
            statusCodeStr = rest;
            reasonPhrase = string.Empty;
        }

        if (!int.TryParse(statusCodeStr, out var statusCode))
            return null;

        // Parse headers
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pos = lineEnd + 1;

        while (pos < text.Length)
        {
            var nextLineEnd = text.IndexOf('\n', pos);
            if (nextLineEnd < 0)
                nextLineEnd = text.Length;

            var line = text[pos..nextLineEnd].TrimEnd('\r');

            // Empty line = end of headers
            if (line.Length == 0)
                break;

            var colonIndex = line.IndexOf(':');
            if (colonIndex > 0)
            {
                var name = line[..colonIndex].Trim();
                var value = line[(colonIndex + 1)..].Trim();

                // If duplicate header, append with comma (RFC 9110 §5.3)
                if (headers.TryGetValue(name, out var existing))
                    headers[name] = existing + ", " + value;
                else
                    headers[name] = value;
            }

            pos = nextLineEnd + 1;
        }

        return new HttpResponse
        {
            StatusCode = statusCode,
            ReasonPhrase = reasonPhrase,
            HttpVersion = httpVersion,
            Headers = headers,
            IsEmpty = false
        };
    }
}
