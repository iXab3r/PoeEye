using System;
using System.Collections.Generic;
using Guards;

namespace PoeShared.Scaffolding
{
    public static class DictionaryExtensions
    {
        public static TValue AddOrUpdate<TKey, TValue>(
            this IDictionary<TKey, TValue> enumerable,
            TKey key,
            TValue newValue,
            Func<TKey, TValue, TValue> updateValueFactory)
        {
            Guard.ArgumentNotNull(enumerable, nameof(enumerable));
            Guard.ArgumentNotNull(key, nameof(key));
            Guard.ArgumentNotNull(newValue, nameof(newValue));
            Guard.ArgumentNotNull(updateValueFactory, nameof(updateValueFactory));

            if (enumerable.TryGetValue(key, out var existing))
            {
                return enumerable[key] = updateValueFactory(key, existing);
            }

            return enumerable[key] = newValue;
        }
    }
}