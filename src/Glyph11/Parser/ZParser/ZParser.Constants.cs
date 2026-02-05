using System.Runtime.CompilerServices;

namespace Glyph11.Parser.ZParser;

[SkipLocalsInit]
public static partial class ZParser
{
    private static ReadOnlySpan<byte> Crlf => "\r\n"u8;

    private const byte Space = 0x20; // ' '
    private const byte Question = 0x3F; // '?'
    private const byte QuerySeparator = 0x26; // '&'
    private const byte Equal = 0x3D; // '='
    private const byte Colon = 0x3A; // ':'
}
