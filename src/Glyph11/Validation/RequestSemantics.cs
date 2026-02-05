using System.Runtime.CompilerServices;
using Glyph11.Protocol;

namespace Glyph11.Validation;

/// <summary>
/// Post-parse semantic validation helpers for HTTP/1.1 requests.
/// </summary>
public static class RequestSemantics
{
    private static ReadOnlySpan<byte> ContentLengthName => "content-length"u8;
    private static ReadOnlySpan<byte> TransferEncodingName => "transfer-encoding"u8;

    /// <summary>
    /// Returns true if the request has multiple Content-Length headers with differing values.
    /// (RFC 9110 §8.6 — request smuggling vector)
    /// </summary>
    public static bool HasConflictingContentLength(BinaryRequest request)
    {
        var headers = request.Headers;
        ReadOnlyMemory<byte> firstValue = default;
        bool found = false;

        for (int i = 0; i < headers.Count; i++)
        {
            var kv = headers[i];
            if (!AsciiEqualsIgnoreCase(kv.Key.Span, ContentLengthName))
                continue;

            if (!found)
            {
                firstValue = kv.Value;
                found = true;
                continue;
            }

            if (!kv.Value.Span.SequenceEqual(firstValue.Span))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the request has both Transfer-Encoding and Content-Length headers.
    /// (RFC 9112 §6.1 — request smuggling vector)
    /// </summary>
    public static bool HasTransferEncodingWithContentLength(BinaryRequest request)
    {
        var headers = request.Headers;
        bool hasTE = false;
        bool hasCL = false;

        for (int i = 0; i < headers.Count; i++)
        {
            var name = headers[i].Key.Span;
            if (!hasTE && AsciiEqualsIgnoreCase(name, TransferEncodingName))
                hasTE = true;
            if (!hasCL && AsciiEqualsIgnoreCase(name, ContentLengthName))
                hasCL = true;
            if (hasTE && hasCL)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the request path contains dot segments (/../, /./, trailing /.. or /.).
    /// (Directory traversal vector)
    /// </summary>
    public static bool HasDotSegments(BinaryRequest request)
    {
        var path = request.Path.Span;
        if (path.Length == 0)
            return false;

        int i = 0;
        while (i < path.Length)
        {
            if (path[i] != (byte)'/')
            {
                i++;
                continue;
            }

            // We're at a '/', check what follows
            int remaining = path.Length - i - 1;

            // "/." at end
            if (remaining == 1 && path[i + 1] == (byte)'.')
                return true;

            // "/.." at end
            if (remaining == 2 && path[i + 1] == (byte)'.' && path[i + 2] == (byte)'.')
                return true;

            // "/./" segment
            if (remaining >= 2 && path[i + 1] == (byte)'.' && path[i + 2] == (byte)'/')
                return true;

            // "/../" segment
            if (remaining >= 3 && path[i + 1] == (byte)'.' && path[i + 2] == (byte)'.' && path[i + 3] == (byte)'/')
                return true;

            i++;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AsciiEqualsIgnoreCase(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        if (a.Length != b.Length)
            return false;

        for (int i = 0; i < a.Length; i++)
        {
            // OR 0x20 lowercases ASCII alpha; for non-alpha it's a no-op or
            // maps to a shared value — but since 'b' is already lowercase,
            // this only matches if 'a' is the same letter (upper or lower).
            if ((a[i] | 0x20) != b[i])
                return false;
        }

        return true;
    }
}
