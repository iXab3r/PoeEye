namespace PoeShared.Scaffolding;

public static class EnumeratorExtensions
{
    public static IReadOnlyList<T> ToList<T>(this IEnumerator<T> enumerator)
    {
        var result = new List<T>();
        while (enumerator.MoveNext())
        {
            result.Add(enumerator.Current);
        }
        return result;
    }
}