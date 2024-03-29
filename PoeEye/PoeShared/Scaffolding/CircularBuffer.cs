﻿namespace PoeShared.Scaffolding;

/// <summary>
///     Circular buffer.
///     When writing to a full buffer:
///     PushBack -> removes this[0] / Front()
///     PushFront -> removes this[Size-1] / Back()
///     this implementation is inspired by
///     http://www.boost.org/doc/libs/1_53_0/libs/circular_buffer/doc/circular_buffer.html
///     because I liked their interface.
/// </summary>
public class CircularBuffer<T> : IReadOnlyList<T>
{
    private readonly T[] buffer;

    /// <summary>
    ///     The _end. Index after the last element in the buffer.
    /// </summary>
    private int end;

    /// <summary>
    ///     The _size. Buffer size.
    /// </summary>
    private int size;

    /// <summary>
    ///     The _start. Index of the first element in buffer.
    /// </summary>
    private int start;

    public CircularBuffer(int capacity)
        : this(capacity, Array.Empty<T>())
    {
    }
    
    public CircularBuffer(T[] items)
        : this(items.Length, items)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CircularBuffer{T}" /> class.
    /// </summary>
    /// <param name='capacity'>
    ///     Buffer capacity. Must be positive.
    /// </param>
    /// <param name='items'>
    ///     Items to fill buffer with. Items length must be less than capacity.
    ///     Suggestion: use Skip(x).Take(y).ToArray() to build this argument from
    ///     any enumerable.
    /// </param>
    public CircularBuffer(int capacity, T[] items)
    {
        if (capacity < 1)
        {
            throw new ArgumentException(
                "Circular buffer cannot have negative or zero capacity.", nameof(capacity));
        }

        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (items.Length > capacity)
        {
            throw new ArgumentException(
                "Too many items to fit circular buffer", nameof(items));
        }

        buffer = new T[capacity];

        Array.Copy(items, buffer, items.Length);
        size = items.Length;

        start = 0;
        end = size == capacity ? 0 : size;
    }

    /// <summary>
    ///     Maximum capacity of the buffer. Elements pushed into the buffer after
    ///     maximum capacity is reached (IsFull = true), will remove an element.
    /// </summary>
    public int Capacity => buffer.Length;

    public bool IsFull => Size == Capacity;

    public bool IsEmpty => Size == 0;

    /// <summary>
    ///     Current buffer size (the number of elements that the buffer has).
    /// </summary>
    public int Size => size;

    public T this[int index]
    {
        get
        {
            if (IsEmpty)
            {
                throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer is empty");
            }

            if (index >= size)
            {
                throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer size is {size}");
            }

            var actualIndex = InternalIndex(index);
            return buffer[actualIndex];
        }
        set
        {
            if (IsEmpty)
            {
                throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer is empty");
            }

            if (index >= size)
            {
                throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer size is {size}");
            }

            var actualIndex = InternalIndex(index);
            buffer[actualIndex] = value;
        }
    }

    public void CopyTo(T[] output)
    {
        var before = ArrayOne();
        var after = ArrayTwo();
        if (before.Count > 0)
        {
            before.CopyTo(output, 0);
        }
        if (after.Count > 0)
        {
            after.CopyTo(output, before.Count - 1);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        var segments = new[] {ArrayOne(), ArrayTwo()};
        foreach (var segment in segments)
        {
            for (var i = 0; i < segment.Count; i++)
            {
                yield return segment.Array[segment.Offset + i];
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    ///     Element at the front of the buffer - this[0].
    /// </summary>
    /// <returns>The value of the element of type T at the front of the buffer.</returns>
    public T Front()
    {
        ThrowIfEmpty();
        return buffer[start];
    }

    /// <summary>
    ///     Element at the back of the buffer - this[Size - 1].
    /// </summary>
    /// <returns>The value of the element of type T at the back of the buffer.</returns>
    public T Back()
    {
        ThrowIfEmpty();
        return buffer[(end != 0 ? end : size) - 1];
    }

    /// <summary>
    ///     Pushes a new element to the back of the buffer. Back()/this[Size-1]
    ///     will now return this element.
    ///     When the buffer is full, the element at Front()/this[0] will be
    ///     popped to allow for this new element to fit.
    /// </summary>
    /// <param name="item">Item to push to the back of the buffer</param>
    public void PushBack(T item)
    {
        if (IsFull)
        {
            buffer[end] = item;
            Increment(ref end);
            start = end;
        }
        else
        {
            buffer[end] = item;
            Increment(ref end);
            ++size;
        }
    }

    /// <summary>
    ///     Pushes a new element to the front of the buffer. Front()/this[0]
    ///     will now return this element.
    ///     When the buffer is full, the element at Back()/this[Size-1] will be
    ///     popped to allow for this new element to fit.
    /// </summary>
    /// <param name="item">Item to push to the front of the buffer</param>
    public void PushFront(T item)
    {
        if (IsFull)
        {
            Decrement(ref start);
            end = start;
            buffer[start] = item;
        }
        else
        {
            Decrement(ref start);
            buffer[start] = item;
            ++size;
        }
    }

    /// <summary>
    ///     Removes the element at the back of the buffer. Decreasing the
    ///     Buffer size by 1.
    /// </summary>
    public T PopBack()
    {
        ThrowIfEmpty("Cannot take elements from an empty buffer.");

        var result = Back();
        Decrement(ref end);
        buffer[end] = default(T);
        --size;
        return result;
    }

    /// <summary>
    ///     Removes the element at the front of the buffer. Decreasing the
    ///     Buffer size by 1.
    /// </summary>
    public void PopFront()
    {
        ThrowIfEmpty("Cannot take elements from an empty buffer.");
        buffer[start] = default(T);
        Increment(ref start);
        --size;
    }

    /// <summary>
    ///     Copies the buffer contents to an array, according to the logical
    ///     contents of the buffer (i.e. independent of the internal
    ///     order/contents)
    /// </summary>
    /// <returns>A new array with a copy of the buffer contents.</returns>
    public T[] ToArray()
    {
        var newArray = new T[Size];
        var newArrayOffset = 0;
        var segments = new ArraySegment<T>[2] {ArrayOne(), ArrayTwo()};
        foreach (var segment in segments)
        {
            Array.Copy(segment.Array, segment.Offset, newArray, newArrayOffset, segment.Count);
            newArrayOffset += segment.Count;
        }

        return newArray;
    }

    /// <summary>
    /// Fills an array with 
    /// </summary>
    public void ClearFill()
    {
        
    }

    private void ThrowIfEmpty(string message = "Cannot access an empty buffer.")
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    ///     Increments the provided index variable by one, wrapping
    ///     around if necessary.
    /// </summary>
    /// <param name="index"></param>
    private void Increment(ref int index)
    {
        if (++index == Capacity)
        {
            index = 0;
        }
    }

    /// <summary>
    ///     Decrements the provided index variable by one, wrapping
    ///     around if necessary.
    /// </summary>
    /// <param name="index"></param>
    private void Decrement(ref int index)
    {
        if (index == 0)
        {
            index = Capacity;
        }

        index--;
    }

    /// <summary>
    ///     Converts the index in the argument to an index in <code>_buffer</code>
    /// </summary>
    /// <returns>
    ///     The transformed index.
    /// </returns>
    /// <param name='index'>
    ///     External index.
    /// </param>
    private int InternalIndex(int index)
    {
        return start + (index < Capacity - start ? index : index - Capacity);
    }

    // doing ArrayOne and ArrayTwo methods returning ArraySegment<T> as seen here: 
    // http://www.boost.org/doc/libs/1_37_0/libs/circular_buffer/doc/circular_buffer.html#classboost_1_1circular__buffer_1957cccdcb0c4ef7d80a34a990065818d
    // http://www.boost.org/doc/libs/1_37_0/libs/circular_buffer/doc/circular_buffer.html#classboost_1_1circular__buffer_1f5081a54afbc2dfc1a7fb20329df7d5b
    // should help a lot with the code.

    // The array is composed by at most two non-contiguous segments, 
    // the next two methods allow easy access to those.

    private ArraySegment<T> ArrayOne()
    {
        return start < end ? new ArraySegment<T>(buffer, start, end - start) : new ArraySegment<T>(buffer, start, buffer.Length - start);
    }

    private ArraySegment<T> ArrayTwo()
    {
        return start < end ? new ArraySegment<T>(buffer, end, 0) : new ArraySegment<T>(buffer, 0, end);
    }

    public int Count => Size;
}