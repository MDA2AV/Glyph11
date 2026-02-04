namespace Glyph11.Utils;

public interface IReadOnlyKeyValueList
{
    int Count { get; }
    
    KeyValuePair<ReadOnlyMemory<byte>,ReadOnlyMemory<byte>> this[int index] { get; }
}

public interface IKeyValueList : IReadOnlyKeyValueList
{
    void Add(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value);

    void Clear();
}