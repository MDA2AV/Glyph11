namespace Glyph11;

/// <summary>
/// Thrown when an HTTP/1.1 request violates protocol rules during parsing or semantic validation.
/// </summary>
public class HttpParseException : Exception
{
    /// <param name="message">A description of the protocol violation.</param>
    public HttpParseException(string message) : base(message) { }
}
