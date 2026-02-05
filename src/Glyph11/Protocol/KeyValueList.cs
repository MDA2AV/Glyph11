using System.Buffers;
using System.Runtime.CompilerServices;

namespace Glyph11.Protocol;

/// <summary>
/// A pooled, growable list of <see cref="KeyValuePair{TKey,TValue}"/> where both key and value
/// are <see cref="ReadOnlyMemory{T}"/> byte slices. Used to store parsed HTTP headers
/// and query parameters without allocating per-item.
/// <para>
/// Backing storage is rented from <see cref="ArrayPool{T}.Shared"/> and doubled on overflow.
/// Call <see cref="Clear"/> between requests to reset without releasing the array,
/// or <see cref="Dispose"/> to return the array to the pool.
/// </para>
/// </summary>
public sealed class KeyValueList : IDisposable
{
    private KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>[] _items;

    private int _count;

    /// <summary>
    /// Creates a new list with an initial pooled capacity.
    /// </summary>
    /// <param name="initialCapacity">Minimum number of entries before the first resize. Actual capacity may be larger due to pool bucket sizing.</param>
    public KeyValueList(int initialCapacity = 16)
    {
        if (initialCapacity <= 0) initialCapacity = 1;
        _items = ArrayPool<KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>>.Shared.Rent(initialCapacity);
        _count = 0;
    }

    /// <summary>Number of key-value pairs currently stored.</summary>
    public int Count => _count;

    /// <summary>
    /// Gets the key-value pair at the specified index.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is out of range.</exception>
    public KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)_count) throw new ArgumentOutOfRangeException(nameof(index));
            return _items[index];
        }
    }

    /// <summary>
    /// Appends a key-value pair. Grows the backing array if needed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Add(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value)
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

    /// <summary>
    /// Cold path: rents a larger array, copies existing entries, returns the old array, then adds.
    /// Kept out-of-line to keep <see cref="Add"/> small enough for inlining.
    /// </summary>
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

    /// <summary>
    /// Clears all entries without releasing the backing array.
    /// Use this between requests when reusing a <see cref="BinaryRequest"/>.
    /// </summary>
    internal void Clear()
    {
        Array.Clear(_items, 0, _count);
        _count = 0;
    }

    /// <summary>
    /// Returns the backing array to <see cref="ArrayPool{T}.Shared"/>.
    /// The list must not be used after disposal.
    /// </summary>
    public void Dispose()
    {
        if (_items is null)
            return;

        Array.Clear(_items, 0, _count);
        ArrayPool<KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>>.Shared.Return(_items);
        _items = null!;
        _count = 0;
    }

    /// <summary>
    /// Returns a read-only span over the populated entries for stack-based enumeration.
    /// </summary>
    public ReadOnlySpan<KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>> AsSpan()
        => _items.AsSpan(0, _count);

}
