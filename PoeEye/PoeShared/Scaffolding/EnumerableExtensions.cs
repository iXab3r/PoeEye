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

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var value in enumerable)
            {
                action(value);
            }
        }

        public static void ForEach<T>(this T[] enumerable, Action<T> action)
        {
            foreach (var value in enumerable)
            {
                action(value);
            }
        }
    }
}