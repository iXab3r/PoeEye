using Guards;

namespace PoeShared.Scaffolding
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class EnumerableExtensions
    {
        private static readonly Random Rng = new Random();

        public static T PickRandom<T>(this IEnumerable<T> enumerable)
        {
            var snapshottedEnumerable = enumerable.ToArray();

            return snapshottedEnumerable.ElementAt(Rng.Next(0, snapshottedEnumerable.Count()));
        }

        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable ?? Enumerable.Empty<T>();
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            Guard.ArgumentNotNull(enumerable, nameof(enumerable));

            foreach (var value in enumerable)
            {
                action(value);
            }
            return enumerable;
        }

        public static IEnumerable<T> ForEach<T>(this T[] enumerable, Action<T> action)
        {
            Guard.ArgumentNotNull(enumerable, nameof(enumerable));

            foreach (var value in enumerable)
            {
                action(value);
            }

            return enumerable;
        }
    }
}
