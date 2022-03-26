namespace PoeShared.Scaffolding;

public sealed class LambdaComparer<T> : IComparer<T>
{
    private readonly Func<T, T, int> comparer;

    public LambdaComparer(Func<T, T, int> comparer)
    {
        this.comparer = comparer;
    }

    public int Compare(T? x, T? y)
    {
        return comparer(x, y);
    }
}