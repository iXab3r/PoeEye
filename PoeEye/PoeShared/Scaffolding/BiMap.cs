#nullable enable

namespace PoeShared.Scaffolding;

[DebuggerDisplay("Count = {Count}")]
public sealed class BiMap<TForward, TReverse> :
    IReadOnlyCollection<KeyValuePair<TForward, TReverse>>
{
    private readonly Dictionary<TForward, TReverse> fwd;
    private readonly Dictionary<TReverse, TForward> rev;

    public BiMap(int capacity = 0,
        IEqualityComparer<TForward>? forwardComparer = null,
        IEqualityComparer<TReverse>? reverseComparer = null)
    {
        fwd = capacity > 0
            ? new Dictionary<TForward, TReverse>(capacity, forwardComparer ?? EqualityComparer<TForward>.Default)
            : new Dictionary<TForward, TReverse>(forwardComparer ?? EqualityComparer<TForward>.Default);

        rev = capacity > 0
            ? new Dictionary<TReverse, TForward>(capacity, reverseComparer ?? EqualityComparer<TReverse>.Default)
            : new Dictionary<TReverse, TForward>(reverseComparer ?? EqualityComparer<TReverse>.Default);
    }

    public BiMap(IEnumerable<KeyValuePair<TForward, TReverse>> pairs,
        IEqualityComparer<TForward>? forwardComparer = null,
        IEqualityComparer<TReverse>? reverseComparer = null)
        : this(0, forwardComparer, reverseComparer)
    {
        foreach (var (k, v) in pairs)
        {
            if (!fwd.TryAdd(k, v))
            {
                throw new ArgumentException($"Duplicate forward key: {k}", nameof(pairs));
            }

            if (!rev.TryAdd(v, k))
            {
                throw new ArgumentException($"Duplicate reverse key: {v}", nameof(pairs));
            }
        }
    }

    public IEqualityComparer<TForward> ForwardComparer => fwd.Comparer;
    public IEqualityComparer<TReverse> ReverseComparer => rev.Comparer;

    // Read-only views (do NOT expose the dictionaries themselves)
    public IReadOnlyDictionary<TForward, TReverse> Forward => fwd;
    public IReadOnlyDictionary<TReverse, TForward> Reverse => rev;

    // Indexers (throw if missing, like Dictionary)
    public TReverse this[TForward key] => fwd[key];
    public TForward this[TReverse key] => rev[key];

    public int Count => fwd.Count;

    public IEnumerator<KeyValuePair<TForward, TReverse>> GetEnumerator()
    {
        return fwd.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    // --- Adds/Updates/Removals ---

    /// <summary>Adds a new 1:1 mapping. Throws if either key already exists.</summary>
    public void Add(TForward forward, TReverse reverse)
    {
        if (!fwd.TryAdd(forward, reverse))
        {
            throw new ArgumentException($"Forward key already exists: {forward}", nameof(forward));
        }

        if (!rev.TryAdd(reverse, forward))
        {
            fwd.Remove(forward); // rollback
            throw new ArgumentException($"Reverse key already exists: {reverse}", nameof(reverse));
        }
    }

    /// <summary>Tries to add a new 1:1 mapping. Returns false if either key exists.</summary>
    public bool TryAdd(TForward forward, TReverse reverse)
    {
        if (!fwd.TryAdd(forward, reverse))
        {
            return false;
        }

        if (!rev.TryAdd(reverse, forward))
        {
            fwd.Remove(forward); // rollback
            return false;
        }

        return true;
    }

    /// <summary>Sets/overwrites mapping, removing any previous associations for either key.</summary>
    public void Set(TForward forward, TReverse reverse)
    {
        // Remove any previous forward mapping
        if (fwd.TryGetValue(forward, out var oldReverse))
        {
            if (EqualityComparer<TReverse>.Default.Equals(oldReverse, reverse))
            {
                return; // already the same mapping
            }

            fwd.Remove(forward);
            rev.Remove(oldReverse);
        }

        // Remove previous reverse mapping (if another forward pointed to 'reverse')
        if (rev.TryGetValue(reverse, out var oldForward))
        {
            rev.Remove(reverse);
            fwd.Remove(oldForward);
        }

        // Now it's safe to add
        fwd.Add(forward, reverse);
        rev.Add(reverse, forward);
    }

    /// <summary>Removes by forward key. Returns true if removed.</summary>
    public bool RemoveByForward(TForward forward)
    {
        if (!fwd.TryGetValue(forward, out var reverse))
        {
            return false;
        }

        fwd.Remove(forward);
        rev.Remove(reverse);
        return true;
    }

    public bool RemoveByReverse(TReverse reverse)
    {
        if (!rev.TryGetValue(reverse, out var forward))
        {
            return false;
        }

        rev.Remove(reverse);
        fwd.Remove(forward);
        return true;
    }

    public void Clear()
    {
        fwd.Clear();
        rev.Clear();
    }

    public bool ContainsForward(TForward forward)
    {
        return fwd.ContainsKey(forward);
    }

    public bool ContainsReverse(TReverse reverse)
    {
        return rev.ContainsKey(reverse);
    }

    public bool TryGetByForward(TForward forward, out TReverse reverse)
    {
        return fwd.TryGetValue(forward, out reverse!);
    }

    public bool TryGetByReverse(TReverse reverse, out TForward forward)
    {
        return rev.TryGetValue(reverse, out forward!);
    }
}