using System.Buffers;
using System.Runtime.CompilerServices;

namespace Glyph11.Parser;

[SkipLocalsInit]
public static partial class Parser11x
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
    private const byte Slash = 0x2F;
    private const byte Dot = 0x2E;

    // ---- HTTP version prefix ----
    private static ReadOnlySpan<byte> HttpSlash => "HTTP/"u8;

    // ---- Cached common HTTP versions (avoids ToArray alloc in ROS path) ----
    private static readonly ReadOnlyMemory<byte> CachedHttp11 = "HTTP/1.1"u8.ToArray();
    private static readonly ReadOnlyMemory<byte> CachedHttp10 = "HTTP/1.0"u8.ToArray();

    /// <summary>
    /// Returns a cached ReadOnlyMemory for HTTP/1.1 and HTTP/1.0, avoiding allocation.
    /// Version must already be validated via IsValidHttpVersionSequence.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlyMemory<byte> ResolveCachedVersion(in ReadOnlySequence<byte> seq)
    {
        byte major, minor;
        if (seq.IsSingleSegment)
        {
            var span = seq.FirstSpan;
            major = span[5];
            minor = span[7];
        }
        else
        {
            Span<byte> buf = stackalloc byte[8];
            seq.CopyTo(buf);
            major = buf[5];
            minor = buf[7];
        }

        if (major == (byte)'1')
        {
            if (minor == (byte)'1') return CachedHttp11;
            if (minor == (byte)'0') return CachedHttp10;
        }

        return seq.ToArray();
    }

    // ---- Token / field-value classification ----
    //
    // Bit 0 (0x01) = token        (RFC 9110 §5.6.2): !#$%&'*+-.^_`|~ DIGIT ALPHA
    // Bit 1 (0x02) = field-value  (RFC 9110 §5.5):   HTAB SP VCHAR obs-text
    //
    // Control chars (0x00-0x08, 0x0A-0x1F, 0x7F) have both bits clear.
    private const byte TkFlag = 0x01;
    private const byte FvFlag = 0x02;

    private static ReadOnlySpan<byte> CharFlags =>
    [
        // 0x00-0x0F
        0, 0, 0, 0, 0, 0, 0, 0, 0, FvFlag, 0, 0, 0, 0, 0, 0,
        // 0x10-0x1F
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        // 0x20-0x2F:  SP ! " # $ % & ' ( ) * + , - . /
        FvFlag, TkFlag | FvFlag, FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag,
        FvFlag, FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, FvFlag,
        // 0x30-0x3F:  0-9 : ; < = > ?
        TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag,
        TkFlag | FvFlag, TkFlag | FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag,
        // 0x40-0x4F:  @ A-O
        FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag,
        TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag,
        // 0x50-0x5F:  P-Z [ \ ] ^ _
        TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag,
        TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, FvFlag, FvFlag, FvFlag, TkFlag | FvFlag, TkFlag | FvFlag,
        // 0x60-0x6F:  ` a-o
        TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag,
        TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag,
        // 0x70-0x7F:  p-z { | } ~ DEL
        TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag,
        TkFlag | FvFlag, TkFlag | FvFlag, TkFlag | FvFlag, FvFlag, TkFlag | FvFlag, FvFlag, TkFlag | FvFlag, 0,
        // 0x80-0xFF: obs-text — allowed in field-value only
        FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag,
        FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag,
        FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag,
        FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag,
        FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag,
        FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag,
        FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag,
        FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag, FvFlag,
    ];

    // ---- Validation helpers (Span) ----

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidToken(ReadOnlySpan<byte> span)
    {
        var flags = CharFlags;
        for (int i = 0; i < span.Length; i++)
        {
            if ((flags[span[i]] & TkFlag) == 0)
                return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidFieldValue(ReadOnlySpan<byte> span)
    {
        var flags = CharFlags;
        for (int i = 0; i < span.Length; i++)
        {
            if ((flags[span[i]] & FvFlag) == 0)
                return false;
        }
        return true;
    }

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
            && IsDigit(span[5])
            && span[6] == (byte)'.'
            && IsDigit(span[7]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDigit(byte b) => (uint)(b - '0') <= 9;

    // ---- Validation helpers (ReadOnlySequence) ----

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidTokenSequence(in ReadOnlySequence<byte> seq)
    {
        if (seq.IsSingleSegment)
            return IsValidToken(seq.FirstSpan);

        var position = seq.Start;
        var end = seq.End;
        while (seq.TryGet(ref position, out ReadOnlyMemory<byte> memory))
        {
            if (!IsValidToken(memory.Span))
                return false;
            if (position.Equals(end))
                break;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidFieldValueSequence(in ReadOnlySequence<byte> seq)
    {
        if (seq.IsSingleSegment)
            return IsValidFieldValue(seq.FirstSpan);

        var position = seq.Start;
        var end = seq.End;
        while (seq.TryGet(ref position, out ReadOnlyMemory<byte> memory))
        {
            if (!IsValidFieldValue(memory.Span))
                return false;
            if (position.Equals(end))
                break;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidHttpVersionSequence(in ReadOnlySequence<byte> seq)
    {
        if (seq.Length != 8)
            return false;
        if (seq.IsSingleSegment)
            return IsValidHttpVersion(seq.FirstSpan);

        Span<byte> buf = stackalloc byte[8];
        seq.CopyTo(buf);
        return IsValidHttpVersion(buf);
    }
}
