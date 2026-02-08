using Glyph11.Protocol;
using Glyph11.Validation;

namespace Glyph11.Parser.Hardened;

public static partial class HardenedParser
{
    private static ReadOnlySpan<byte> TransferEncodingName => "transfer-encoding"u8;
    private static ReadOnlySpan<byte> ChunkedValue => "chunked"u8;

    /// <summary>
    /// Inspects the parsed headers in <paramref name="request"/> and returns the body
    /// framing kind (chunked, content-length, or none) without touching any body bytes.
    /// </summary>
    public static BodyFramingResult DetectBodyFraming(BinaryRequest request)
    {
        if (HasChunkedTE(request))
            return BodyFramingResult.ForChunked;

        long cl = ContentLengthBodyReader.ParseContentLength(request);
        if (cl > 0)
            return BodyFramingResult.ForContentLength(cl);

        return BodyFramingResult.NoBody;
    }

    private static bool HasChunkedTE(BinaryRequest request)
    {
        var headers = request.Headers;

        for (int i = 0; i < headers.Count; i++)
        {
            var name = headers[i].Key.Span;
            if (!AsciiEqualsIgnoreCase(name, TransferEncodingName))
                continue;

            var value = headers[i].Value.Span;

            // Trim OWS
            int start = 0;
            while (start < value.Length && (value[start] == (byte)' ' || value[start] == (byte)'\t'))
                start++;
            int end = value.Length;
            while (end > start && (value[end - 1] == (byte)' ' || value[end - 1] == (byte)'\t'))
                end--;

            var trimmed = value[start..end];
            if (AsciiEqualsIgnoreCase(trimmed, ChunkedValue))
                return true;
        }

        return false;
    }

    private static bool AsciiEqualsIgnoreCase(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if ((a[i] | 0x20) != (b[i] | 0x20)) return false;
        }
        return true;
    }
}
