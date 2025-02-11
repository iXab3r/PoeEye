namespace PoeShared.Scaffolding;

public static class SetExtensions
{
    /// <summary>
    ///     Attempts to add multiple items to the specified set and returns whether any item was successfully added.
    /// </summary>
    /// <typeparam name="T">The type of elements contained in the set.</typeparam>
    /// <param name="set">The set to which items will be added.</param>
    /// <param name="items">The collection of items to add to the set.</param>
    /// <returns>
    ///     <c>true</c> if at least one item was successfully added to the set; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if either <paramref name="set" /> or <paramref name="items" /> is <c>null</c>.
    /// </exception>
    public static bool AddAny<T>(this ISet<T> set, IEnumerable<T> items)
    {
        if (set == null)
        {
            throw new ArgumentNullException(nameof(set));
        }

        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        return items.Aggregate(false, (b, item) => b || set.Add(item));
    }
}