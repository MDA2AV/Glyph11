namespace Glyph11.ProprietaryCollections;

public interface IKeyValueList
{
    int Count { get; }
    
    KeyValuePair<ReadOnlyMemory<byte>,ReadOnlyMemory<byte>> this[int index] { get; }
    
    void Add(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value);

    void Clear();
}