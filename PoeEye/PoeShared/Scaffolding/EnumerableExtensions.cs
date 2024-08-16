using System.Collections.ObjectModel;
using Combinatorics.Collections;


namespace PoeShared.Scaffolding;

public static class EnumerableExtensions
{
    private static readonly Random Rng = new Random();

    public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
    {
        return !enumerable.Any();
    }
    
    public static async Task<TAccumulate> AggregateAsync<TSource, TAccumulate>(
        this IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, Task<TAccumulate>> func)
    {
        var result = seed;
        foreach (var item in source)
        {
            result = await func(result, item);
        }
        return result;
    }

    /// <summary>
    /// Inserts a specified separator between each element of a sequence, if the sequence has more than one item.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the input sequence.</typeparam>
    /// <param name="items">The sequence into which the separator will be interspersed.</param>
    /// <param name="separatorFactory">The element factory which will be used to intersperse in between the elements of the sequence.</param>
    /// <returns>A sequence that contains the elements of the input sequence with the separator interspersed between each element.</returns>
    /// <remarks>
    /// If the sequence is empty or contains only one element, the returned sequence will be equivalent to the input sequence.
    /// </remarks>
    public static IEnumerable<T> Intersperse<T>(this IEnumerable<T> items, Func<T, T> separatorFactory)
    {
        if (!items.Any() || items.Count() == 1)
        {
            return items;
        }
        return items.SelectMany((item, index) => index == items.Count() - 1 ? new[] { item } : new[] { item, separatorFactory(item) });
    }
    
    /// <summary>
    /// Inserts a specified separator between each element of a sequence, if the sequence has more than one item.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the input sequence.</typeparam>
    /// <param name="items">The sequence into which the separator will be interspersed.</param>
    /// <param name="separator">The element to intersperse in between the elements of the sequence.</param>
    /// <returns>A sequence that contains the elements of the input sequence with the separator interspersed between each element.</returns>
    /// <remarks>
    /// If the sequence is empty or contains only one element, the returned sequence will be equivalent to the input sequence.
    /// </remarks>
    public static IEnumerable<T> Intersperse<T>(this IEnumerable<T> items, T separator)
    {
        return Intersperse(items, _ => separator);
    }
    
    /// <summary>
    /// Returns a specified percentage of elements from the start of an IList<T>.
    /// </summary>
    /// <param name="list">The list from which to pick elements.</param>
    /// <param name="percentage">The percentage of elements to return.</param>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <returns>A list containing the specified percentage of elements from the start of the original list.</returns>
    public static IReadOnlyList<T> PickPercentage<T>(this IList<T> list, double percentage)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list));
        }
        if (percentage is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage), $"Percentage must be between 0 and 100, but was {percentage}");
        }

        // Calculate the number of elements to return
        var elementsToReturn = (int)Math.Round(list.Count * (percentage / 100.0));

        // Return the specified percentage from the start of the list
        var resultList = new List<T>();
        for (var i = 0; i < elementsToReturn; i++)
        {
            resultList.Add(list[i]);
        }

        return resultList;
    }

    public static T PickRandom<T>(this IEnumerable<T> enumerable)
    {
        Guard.ArgumentNotNull(enumerable, nameof(enumerable));

        var snapshottedEnumerable = enumerable.ToArray();

        return snapshottedEnumerable.ElementAt(Rng.Next(0, snapshottedEnumerable.Count()));
    }
    
    public static IEnumerable<T> Randomize<T>(this IEnumerable<T> source)
    {
        var elements = source.ToArray();
        for (var i = elements.Length - 1; i >= 0; i--)
        {
            // Swap element "i" with a random earlier element including itself
            var swapIndex = Rng.Next(i + 1);
            yield return elements[swapIndex];
            elements[swapIndex] = elements[i];
        }
    }

    public static IEnumerable<T> Subrange<T>(this IReadOnlyList<T> enumerable, int startIdx, int count)
    {
        Guard.ArgumentNotNull(enumerable, nameof(enumerable));
        for (var idx = startIdx; idx - startIdx < count; idx++)
        {
            yield return enumerable[idx];
        }
    }

    public static IEnumerable<IEnumerable<T>> Transpose<T>(
        this IEnumerable<IEnumerable<T>> source)
    {
        var enumerators = source.Select(e => e.GetEnumerator()).ToArray();
        try
        {
            while (enumerators.All(e => e.MoveNext()))
            {
                yield return enumerators.Select(e => e.Current).ToArray();
            }
        }
        finally
        {
            Array.ForEach(enumerators, e => e.Dispose());
        }
    }

    public static Task ForEachAsync<T>(this IEnumerable<T> enumerable, Func<T, Task> action)
    {
        return Task.WhenAll(enumerable.Select(action));
    }
    
    public static T[] EmptyIfNull<T>(this T[] array)
    {
        return array ?? Array.Empty<T>();
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
        
    public static IDictionary<TKey, TValue> ToDictionaryWithReplacement<T, TKey, TValue>(this IEnumerable<T> enumerable,
        Func<T, TKey> keyExtractor,
        Func<T, TValue> valueExtractor)
    {
        return ToDictionary(enumerable, keyExtractor, valueExtractor, tuple => tuple.newValue);
    }
    public static IDictionary<TKey, TValue> ToDictionaryWithThrow<T, TKey, TValue>(this IEnumerable<T> enumerable,
        Func<T, TKey> keyExtractor,
        Func<T, TValue> valueExtractor)
    {
        return ToDictionary(enumerable, keyExtractor, valueExtractor, tuple => throw new ArgumentException($"Dictionary already contains item with key {tuple.key}: {tuple.existingValue}, tried to add value: {tuple.newValue}"));
    }
        
    public static IDictionary<TKey, TValue> ToDictionary<T, TKey, TValue>(
        this IEnumerable<T> enumerable, 
        Func<T, TKey> keyExtractor, 
        Func<T, TValue> valueExtractor,
        Func<(TKey key, TValue existingValue, TValue newValue), TValue> conflictSolver)
    {
        var result = new Dictionary<TKey, TValue>();
        foreach (var item in enumerable)
        {
            var key = keyExtractor(item);
            var newValue = valueExtractor(item);
            if (result.TryGetValue(key, out var existingValue))
            {
                newValue = conflictSolver((key, existingValue, newValue: newValue));
            }

            result[key] = newValue;
        }
        return result;
    }

    public static void DisposeAll<T>(this IEnumerable<T> enumerable, Action<T, Exception> onError = null) where T : IDisposable
    {
        foreach (var disposable in enumerable)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception e)
            {
                if (onError == null)
                {
                    throw;
                }

                onError(disposable, e);
            }
        }
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

    public static T[] ForEach<T>(this T[] enumerable, Action<T> action)
    {
        Guard.ArgumentNotNull(enumerable, nameof(enumerable));

        foreach (var value in enumerable)
        {
            action(value);
        }

        return enumerable;
    }

    public static bool IsUnique<T>(this IEnumerable<T> list)
    {
        var hs = new HashSet<T>();
        return list.All(hs.Add);
    }

    public static IList<IList<T>> ToPermutations<T>(this IList<T> source)
    {
        var permutations = new Permutations<T>(source);
        return permutations.ToList();
    }

    public static IEnumerable<IEnumerable<T>> ToVariations<T>(this IEnumerable<T> source)
    {
        var permutations = Enumerable.Range(1, source.Count()).Select(x => new Variations<T>(source.ToList(), x));
        return permutations.SelectMany(x => x);
    }

    public static IEnumerable<TResult> SelectSafe<T, TResult>(
        this IEnumerable<T> source,
        Func<T, TResult> selector,
        Action<T, Exception> errorConsumer)
    {
        foreach (var item in source)
        {
            TResult result;
            bool success;
            try
            {
                result = selector(item);
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                result = default;
                errorConsumer(item, ex);
            }

            if (success)
            {
                yield return result;
            }
        }
    }
    
    public static IEnumerable<TResult> SelectSafe<T, TResult>(this IEnumerable<T> source, Func<T, TResult> selector)
    {
        return SelectSafe(source, selector, (_, _) => { });
    }
}