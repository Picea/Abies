// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if DEBUG

namespace Picea.Abies.Debugger;

/// <summary>
/// FIFO ring buffer with automatic eviction when capacity exceeded.
/// Maintains strict timestamp monotonicity and sequence integrity.
/// </summary>
public sealed class RingBuffer<T> where T : class
{
    private T?[] _buffer;
    private int _count;
    private int _nextWriteIndex;
    private long _nextSequence;

    public RingBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than 0", nameof(capacity));

        _buffer = new T[capacity];
        _count = 0;
        _nextWriteIndex = 0;
        _nextSequence = 0;
    }

    public int Count => _count;
    public int Capacity => _buffer.Length;
    public long NextSequence => _nextSequence;

    /// <summary>
    /// Gets an entry by index (0-based from oldest to newest).
    /// </summary>
    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
                throw new IndexOutOfRangeException($"Index {index} out of range [0, {_count - 1})");

            if (_count < _buffer.Length)
            {
                // Buffer is not yet full
                return _buffer[index]!;
            }

            // Buffer is full, wrap around from _nextWriteIndex
            int actualIndex = (_nextWriteIndex + index) % _buffer.Length;
            return _buffer[actualIndex]!;
        }
    }

    /// <summary>
    /// Gets all entries in FIFO order (oldest to newest).
    /// </summary>
    public IReadOnlyList<T> Entries => GetAllInternal();

    public void Add(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        _buffer[_nextWriteIndex] = item;

        // Advance write pointer
        _nextWriteIndex = (_nextWriteIndex + 1) % _buffer.Length;

        // Increment count until we reach capacity
        if (_count < _buffer.Length)
        {
            _count++;
        }

        _nextSequence++;
    }

    public void Clear()
    {
        for (int i = 0; i < _buffer.Length; i++)
        {
            _buffer[i] = null;
        }

        _count = 0;
        _nextWriteIndex = 0;
    }

    private IReadOnlyList<T> GetAllInternal()
    {
        if (_count == 0)
            return [];

        var result = new T[_count];

        if (_count < _buffer.Length)
        {
            // Buffer is not yet full, entries are at indices 0 to _count-1
            for (int i = 0; i < _count; i++)
            {
                result[i] = _buffer[i]!;
            }
        }
        else
        {
            // Buffer is full, wrap around starting from _nextWriteIndex
            int sourceIdx = _nextWriteIndex;
            for (int i = 0; i < _count; i++)
            {
                result[i] = _buffer[sourceIdx]!;
                sourceIdx = (sourceIdx + 1) % _buffer.Length;
            }
        }

        return result;
    }
}

#endif
