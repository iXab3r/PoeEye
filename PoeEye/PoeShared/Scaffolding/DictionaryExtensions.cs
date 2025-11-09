namespace PoeShared.Scaffolding;

public static class DictionaryExtensions
{
    public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
    {
        if (dictionary.TryGetValue(key, out var result))
        {
            return result;
        }

        return default;
    }
    
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory)
    {
        if (dictionary.TryGetValue(key, out var result))
        {
            return result;
        }

        var newValue = valueFactory(key);
        dictionary[key] = newValue;
        return newValue;
    }

    public static void Add<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        KeyValuePair<TKey, TValue> item)
    {
        dictionary.Add(item.Key, item.Value);
    }


    public static TValue AddOrUpdate<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TValue> newValueFactory)
    {
        return AddOrUpdate(dictionary, key, newValueFactory, (_, _) => newValueFactory());
    }

    public static TValue AddOrUpdate<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TValue> newValueFactory,
        Func<TKey, TValue, TValue> updateValueFactory)
    {
        Guard.ArgumentNotNull(dictionary, nameof(dictionary));
        Guard.ArgumentNotNull(key, nameof(key));
        Guard.ArgumentNotNull(newValueFactory, nameof(newValueFactory));
        Guard.ArgumentNotNull(updateValueFactory, nameof(updateValueFactory));

        if (dictionary.TryGetValue(key, out var existing))
        {
            return dictionary[key] = updateValueFactory(key, existing);
        }

        return dictionary[key] = newValueFactory();
    }

    public static bool TryRemove<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        out TValue value)
    {
        return dictionary.Remove(key, out value);
    }

    public static void EditDiff<TKey>(
        this IDictionary<TKey, TKey> dictionary,
        IEnumerable<TKey> newItems)
    {
        var itemsToProcess = newItems.ToArray();
        var newKeys = itemsToProcess.ToHashSet();

        // Remove items not present in newItems
        var keysToRemove = dictionary.Keys.Where(k => !newKeys.Contains(k)).ToArray();
        foreach (var key in keysToRemove)
        {
            dictionary.Remove(key);
        }

        // Add or update items from newItems
        foreach (var item in itemsToProcess)
        {
            dictionary[item] = item;
        }
    }
}