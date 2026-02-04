namespace Glyph11.Utils;

public interface IKeyValueList
{
    int Count { get; }
    
    KeyValuePair<ReadOnlyMemory<byte>,ReadOnlyMemory<byte>> this[int index] { get; }
}