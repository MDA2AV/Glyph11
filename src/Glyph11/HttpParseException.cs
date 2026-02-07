namespace Glyph11;

/// <summary>
/// Thrown when an HTTP/1.1 request violates protocol rules during parsing or semantic validation.
/// </summary>
public class HttpParseException : Exception
{
    /// <param name="message">A description of the protocol violation.</param>
    public HttpParseException(string message, bool isLimitViolation = false) : base(message)
    {
        IsLimitViolation = isLimitViolation;
    }

    /// <summary>True when the error is a size/count limit breach (→ 431), false for structural errors (→ 400).</summary>
    public bool IsLimitViolation { get; }
}
