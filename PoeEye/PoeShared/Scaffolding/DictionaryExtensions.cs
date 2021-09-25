using System;
using System.Collections.Generic;


namespace PoeShared.Scaffolding
{
    public static class DictionaryExtensions
    {
        public static TValue AddOrUpdate<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue newValue,
            Func<TKey, TValue, TValue> updateValueFactory)
        {
            Guard.ArgumentNotNull(dictionary, nameof(dictionary));
            Guard.ArgumentNotNull(key, nameof(key));
            Guard.ArgumentNotNull(newValue, nameof(newValue));
            Guard.ArgumentNotNull(updateValueFactory, nameof(updateValueFactory));

            if (dictionary.TryGetValue(key, out var existing))
            {
                return dictionary[key] = updateValueFactory(key, existing);
            }

            return dictionary[key] = newValue;
        }

        public static bool TryRemove<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            out TValue value)
        {
            return dictionary.Remove(key, out value);
        }
    }
}