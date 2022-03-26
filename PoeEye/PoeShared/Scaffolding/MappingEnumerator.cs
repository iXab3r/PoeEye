namespace PoeShared.Scaffolding;

public sealed class MappingEnumerator<T, TResult> : IEnumerator<TResult>
{
    private readonly Func<T, TResult> selector;
    private readonly IEnumerator<T> enumerator;

    public MappingEnumerator(
        IEnumerator<T> enumerator,
        Func<T,TResult> selector)
    {
        this.enumerator = enumerator;
        this.selector = selector;
    }

    public bool MoveNext()
    {
        return enumerator.MoveNext();
    }

    public void Reset()
    {
        enumerator.Reset();
    }

    public TResult Current => selector(enumerator.Current);

    object IEnumerator.Current => Current;

    public void Dispose()
    {
        enumerator.Dispose();
    }
}