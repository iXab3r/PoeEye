using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;


namespace PoeShared.Scaffolding
{
    public static class EnumerableExtensions
    {
        private static readonly Random Rng = new Random();

        public static T PickRandom<T>(this IEnumerable<T> enumerable)
        {
            Guard.ArgumentNotNull(enumerable, nameof(enumerable));

            var snapshottedEnumerable = enumerable.ToArray();

            return snapshottedEnumerable.ElementAt(Rng.Next(0, snapshottedEnumerable.Count()));
        }

        public static IEnumerable<T> Subrange<T>(this IReadOnlyList<T> enumerable, int startIdx, int count)
        {
            Guard.ArgumentNotNull(enumerable, nameof(enumerable));
            for (var idx = startIdx; idx - startIdx < count; idx++)
            {
                yield return enumerable[idx];
            }
        }

        public static Task ForEachAsync<T>(this IEnumerable<T> enumerable, Func<T, Task> action)
        {
            return Task.WhenAll(enumerable.Select(action));
        }

        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable ?? Enumerable.Empty<T>();
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> enumerable)
        {
            return new ObservableCollection<T>(enumerable);
        }

        public static ReadOnlyObservableCollection<T> ToReadOnlyObservableCollection<T>(this IEnumerable<T> enumerable)
        {
            return new ReadOnlyObservableCollection<T>(enumerable.ToObservableCollection());
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