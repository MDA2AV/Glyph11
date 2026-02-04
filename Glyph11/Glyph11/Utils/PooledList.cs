using System.Buffers;

namespace Glyph11.Utils;

public sealed class PooledList<T> : IDisposable
{
    private T[] _items;
    public int Count { get; private set; }

    public PooledList(int initialCapacity = 8)
    {
        _items = ArrayPool<T>.Shared.Rent(initialCapacity);
    }

    public void Add(T item)
    {
        if (Count == _items.Length)
            Grow();

        _items[Count++] = item;
    }

    public ReadOnlySpan<T> AsSpan() => _items.AsSpan(0, Count);

    private void Grow()
    {
        var newArr = ArrayPool<T>.Shared.Rent(_items.Length * 2);
        Array.Copy(_items, newArr, Count);
        ArrayPool<T>.Shared.Return(_items, clearArray: true);
        _items = newArr;
    }

    public void Clear()
    {
        Array.Clear(_items, 0, Count);
        Count = 0;
    }

    public void Dispose()
    {
        if (_items != null)
        {
            ArrayPool<T>.Shared.Return(_items, clearArray: true);
            _items = null!;
        }
    }
}
