namespace GenHTTP.Api.Draft.Protocol.Raw;

public interface IRawKeyValueList
{

    int Count { get; }

    KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>> this[int index] { get; }

}
