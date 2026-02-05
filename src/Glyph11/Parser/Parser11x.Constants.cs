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
        if (span.Length != 8)
            return false;
        if (!span[..5].SequenceEqual(HttpSlash))
            return false;
        if (!IsDigit(span[5]) || span[6] != (byte)'.' || !IsDigit(span[7]))
            return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDigit(byte b) => (uint)(b - '0') <= 9;

    // ---- Validation helpers (ReadOnlySequence) ----

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidTokenSequence(in ReadOnlySequence<byte> seq)
    {
        if (seq.IsSingleSegment)
            return IsValidToken(seq.FirstSpan);

        foreach (var segment in seq)
        {
            if (!IsValidToken(segment.Span))
                return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidFieldValueSequence(in ReadOnlySequence<byte> seq)
    {
        if (seq.IsSingleSegment)
            return IsValidFieldValue(seq.FirstSpan);

        foreach (var segment in seq)
        {
            if (!IsValidFieldValue(segment.Span))
                return false;
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
