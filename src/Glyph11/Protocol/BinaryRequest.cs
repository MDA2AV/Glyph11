namespace Glyph11.Protocol;

public class BinaryRequest : IDisposable
{
    private readonly KeyValueList _headers = new(), _query = new();

    public ReadOnlyMemory<byte> Version { get; internal set; }

    public ReadOnlyMemory<byte> Method { get; internal set; }

    public ReadOnlyMemory<byte> Path { get; internal set; }

    public KeyValueList QueryParameters => _query;

    public KeyValueList Headers => _headers;

    public ReadOnlyMemory<byte> Body { get; internal set; }

    public void Clear()
    {
        // todo
    }

    public void Dispose()
    {
        _headers.Dispose();
        _query.Dispose();
    }

}
