#nullable enable
using System.Collections.Immutable;

namespace PoeShared.Scaffolding;

/// <summary>
/// Ref-counted MRU set: items are unique by equality; each Add increments a refcount,
/// each token Dispose decrements it; item is removed when refcount hits zero.
/// Enumeration yields items in MRU-first order (most recently added/updated first).
/// </summary>
public class RefCountedLinkedSet<T> : IEnumerable<T>
{
    private readonly Dictionary<T, (int Count, LinkedListNode<T> Node)> map;
    private readonly LinkedList<T> order;

    public RefCountedLinkedSet() : this(null) { }

    public RefCountedLinkedSet(IEqualityComparer<T>? comparer)
    {
        map = new Dictionary<T, (int, LinkedListNode<T>)>(comparer);
        order = new LinkedList<T>();
    }

    /// <summary>
    /// Adds an item (increments refcount). If it already exists, refcount is incremented
    /// and its recency is bumped to most-recent. Returns a token that decrements the refcount
    /// on <see cref="IDisposable.Dispose"/>.
    /// </summary>
    public IDisposable Add(T? item)
    {
        if (item is null)
        {
            // No-op token keeps usage simple
            return Disposable.Empty;
        }

        if (map.TryGetValue(item, out var entry))
        {
            entry.Count++;
            // bump recency
            order.Remove(entry.Node);
            order.AddLast(entry.Node);
            map[item] = entry;
        }
        else
        {
            var node = order.AddLast(item);
            map[item] = (1, node);
        }

        return Disposable.Create(() =>
        {
            if (!map.TryGetValue(item, out var e)) return;
            e.Count--;
            if (e.Count <= 0)
            {
                order.Remove(e.Node);
                map.Remove(item);
            }
            else
            {
                map[item] = e;
            }
        });
    }

    /// <summary>
    /// Returns true if the item exists (refcount &gt; 0).
    /// </summary>
    public bool Contains(T item) => map.ContainsKey(item);

    /// <summary>
    /// Total unique items currently present (i.e., with refcount &gt; 0).
    /// </summary>
    public int Count => map.Count;

    /// <summary>
    /// Removes everything regardless of refcounts.
    /// </summary>
    public void Clear()
    {
        map.Clear();
        order.Clear();
    }

    /// <summary>
    /// Enumerates in MRU-first order (most-recent first).
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        // order maintains LRU..MRU (head..tail). We need MRU-first, so iterate from tail to head.
        for (var node = order.Last; node is not null; node = node.Previous)
        {
            yield return node.Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}