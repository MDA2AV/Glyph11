using System.Buffers;
using System.Runtime.CompilerServices;

namespace Glyph11.Parser.Hardened;

/// <summary>
/// Security-hardened HTTP/1.1 header parser with RFC 9110/9112 validation
/// and configurable resource limits via <see cref="ParserLimits"/>.
/// <para>
/// Validates method and header-name tokens, field-value characters,
/// HTTP version format, and enforces size/count limits. Throws
/// <see cref="HttpParseException"/> on any protocol violation.
/// </para>
/// </summary>
[SkipLocalsInit]
public static partial class HardenedParser
{
    // ---- Line terminators ----
    private static ReadOnlySpan<byte> Crlf => "\r\n"u8;
    private static ReadOnlySpan<byte> CrlfCrlf => "\r\n\r\n"u8;

    // ---- Special bytes ----
    private const byte Space = 0x20;
    private const byte Question = 0x3F;
    private const byte QuerySeparator = 0x26; // '&'
    private const byte Equal = 0x3D;
    private const byte Colon = 0x3A;

    // ---- SIMD-accelerated character class validators (SearchValues<byte>) ----

    // Token chars (RFC 9110 §5.6.2): !#$%&'*+-.^_`|~ DIGIT ALPHA
    private static readonly SearchValues<byte> TokenSearchValues = SearchValues.Create(
        "!#$%&'*+-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ^_`abcdefghijklmnopqrstuvwxyz|~"u8);

    // Field-value chars (RFC 9110 §5.5): HTAB SP VCHAR obs-text
    private static readonly SearchValues<byte> FieldValueSearchValues = SearchValues.Create(
        BuildFieldValueBytes());

    // Request-target: reject control chars (0x00-0x1F, 0x7F)
    private static readonly SearchValues<byte> RequestTargetSearchValues = SearchValues.Create(
        BuildRequestTargetBytes());

    private static byte[] BuildFieldValueBytes()
    {
        var bytes = new byte[1 + (0x7E - 0x20 + 1) + (0xFF - 0x80 + 1)];
        int i = 0;
        bytes[i++] = 0x09; // HTAB
        for (int b = 0x20; b <= 0x7E; b++) bytes[i++] = (byte)b; // SP + VCHAR
        for (int b = 0x80; b <= 0xFF; b++) bytes[i++] = (byte)b; // obs-text
        return bytes;
    }

    private static byte[] BuildRequestTargetBytes()
    {
        var bytes = new byte[(0x7E - 0x20 + 1) + (0xFF - 0x80 + 1)];
        int i = 0;
        for (int b = 0x20; b <= 0x7E; b++) bytes[i++] = (byte)b;
        for (int b = 0x80; b <= 0xFF; b++) bytes[i++] = (byte)b;
        return bytes;
    }

    // ---- Validation helpers (Span) ----

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidToken(ReadOnlySpan<byte> span)
        => span.IndexOfAnyExcept(TokenSearchValues) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidFieldValue(ReadOnlySpan<byte> span)
        => span.IndexOfAnyExcept(FieldValueSearchValues) < 0;

    /// <summary>
    /// HTTP-version = "HTTP/" DIGIT "." DIGIT  (RFC 9112 §2.6)
    /// Exactly 8 bytes, case-sensitive.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidHttpVersion(ReadOnlySpan<byte> span)
    {
        return span.Length == 8
            && span[0] == (byte)'H'
            && span[1] == (byte)'T'
            && span[2] == (byte)'T'
            && span[3] == (byte)'P'
            && span[4] == (byte)'/'
            && span[5] == (byte)'1'
            && span[6] == (byte)'.'
            && (span[7] == (byte)'0' || span[7] == (byte)'1');
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDigit(byte b) => (uint)(b - '0') <= 9;

    /// <summary>
    /// Validates that a request-target contains no control characters (0x00-0x1F, 0x7F).
    /// RFC 9112 §3.2 — request-target must only contain VCHAR and unreserved/reserved URI chars.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidRequestTarget(ReadOnlySpan<byte> span)
        => span.IndexOfAnyExcept(RequestTargetSearchValues) < 0;

    /// <summary>
    /// Case-insensitive match for "content-length" (14 bytes).
    /// Uses OR 0x20 to lowercase ASCII letters; short-circuits on first mismatch.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsContentLength(ReadOnlySpan<byte> name)
        => name.Length == 14
            && (name[0]  | 0x20) == 'c'
            && (name[1]  | 0x20) == 'o'
            && (name[2]  | 0x20) == 'n'
            && (name[3]  | 0x20) == 't'
            && (name[4]  | 0x20) == 'e'
            && (name[5]  | 0x20) == 'n'
            && (name[6]  | 0x20) == 't'
            && name[7]           == '-'
            && (name[8]  | 0x20) == 'l'
            && (name[9]  | 0x20) == 'e'
            && (name[10] | 0x20) == 'n'
            && (name[11] | 0x20) == 'g'
            && (name[12] | 0x20) == 't'
            && (name[13] | 0x20) == 'h';

    /// <summary>
    /// Validates Content-Length value: 1*DIGIT with optional comma-separated duplicates
    /// per RFC 9112 §6.2. Rejects empty, leading zeros (except bare "0"), negative/minus,
    /// and non-digit characters.
    /// </summary>
    private static bool IsValidContentLengthValue(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty) return false;

        int pos = 0;
        while (pos < value.Length)
        {
            // Skip OWS before each element
            while (pos < value.Length && (value[pos] == (byte)' ' || value[pos] == (byte)'\t'))
                pos++;

            if (pos >= value.Length) return false;

            // Must start with a digit
            if (!IsDigit(value[pos])) return false;

            // Reject leading zeros: "0" is ok, "00" or "007" is not
            int digitStart = pos;
            if (value[pos] == (byte)'0')
            {
                pos++;
                if (pos < value.Length && IsDigit(value[pos]))
                    return false; // leading zero
            }
            else
            {
                pos++;
                while (pos < value.Length && IsDigit(value[pos]))
                    pos++;
            }

            // Skip OWS after the number
            while (pos < value.Length && (value[pos] == (byte)' ' || value[pos] == (byte)'\t'))
                pos++;

            // Must be end or comma
            if (pos >= value.Length) return true;
            if (value[pos] != (byte)',') return false;
            pos++; // skip comma
        }

        return false; // trailing comma with nothing after
    }

}
