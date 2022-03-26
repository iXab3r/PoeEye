namespace PoeShared.Scaffolding;

public sealed class LambdaEqualityComparer<T> : IEqualityComparer<T>
{
    private readonly Func<T, T, bool> comparer;
    private readonly Func<T, int> hash;

    public LambdaEqualityComparer(Func<T, T, bool> comparer)
        : this(comparer, t => 0)
    {
    }

    public LambdaEqualityComparer(Func<T, T, bool> comparer, Func<T, int> hash)
    {
        this.comparer = comparer;
        this.hash = hash;
    }

    public bool Equals(T x, T y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (ReferenceEquals(x, null))
        {
            return false;
        }

        if (ReferenceEquals(y, null))
        {
            return false;
        }

        return comparer(x, y);
    }

    public int GetHashCode(T obj)
    {
        return hash(obj);
    }
}