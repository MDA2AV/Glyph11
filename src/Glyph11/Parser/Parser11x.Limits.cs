namespace Glyph11.Parser;

/// <summary>
/// Resource limits for the security-hardened HTTP/1.1 parser.
/// </summary>
public readonly record struct ParserLimits
{
    public int MaxHeaderCount { get; init; }
    public int MaxHeaderNameLength { get; init; }
    public int MaxHeaderValueLength { get; init; }
    public int MaxUrlLength { get; init; }
    public int MaxQueryParameterCount { get; init; }
    public int MaxMethodLength { get; init; }
    public int MaxTotalHeaderBytes { get; init; }

    public static ParserLimits Default => new()
    {
        MaxHeaderCount = 100,
        MaxHeaderNameLength = 256,
        MaxHeaderValueLength = 8192,
        MaxUrlLength = 8192,
        MaxQueryParameterCount = 128,
        MaxMethodLength = 16,
        MaxTotalHeaderBytes = 32768
    };
}
