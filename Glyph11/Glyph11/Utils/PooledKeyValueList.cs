using System.Buffers;
using System.Runtime.CompilerServices;

namespace Glyph11.Utils;

public sealed class PooledKeyValueList : IKeyValueList, IDisposable
{
    private KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>[] _items;
    private int _count;

    public PooledKeyValueList(int initialCapacity = 16)
    {
        if (initialCapacity <= 0) initialCapacity = 1;
        _items = ArrayPool<KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>>.Shared.Rent(initialCapacity);
        _count = 0;
    }

    public int Count => _count;

    public KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)_count) throw new ArgumentOutOfRangeException(nameof(index));
            return _items[index];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value)
    {
        int i = _count;
        if ((uint)i < (uint)_items.Length)
        {
            _items[i] = new KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(key, value);
            _count = i + 1;
            return;
        }

        GrowAndAdd(key, value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndAdd(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value)
    {
        int newSize = _items.Length * 2;
        if (newSize < 16) newSize = 16;

        var newArr = ArrayPool<KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>>.Shared.Rent(newSize);
        Array.Copy(_items, 0, newArr, 0, _count);

        // Clear old entries before returning (ReadOnlyMemory holds refs to owners)
        Array.Clear(_items, 0, _count);
        ArrayPool<KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>>.Shared.Return(_items);

        _items = newArr;

        _items[_count++] = new KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(key, value);
    }

    public void Clear()
    {
        // Important: clear references so buffers/owners can be GC’d
        Array.Clear(_items, 0, _count);
        _count = 0;
    }

    public void Dispose()
    {
        if (_items is null) 
            return;
        
        Array.Clear(_items, 0, _count);
        ArrayPool<KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>>.Shared.Return(_items);
        _items = null!;
        _count = 0;
    }
    
    public ReadOnlySpan<KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>> AsSpan()
        => _items.AsSpan(0, _count);
}
