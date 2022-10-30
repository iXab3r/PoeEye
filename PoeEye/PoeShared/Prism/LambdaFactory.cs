using PoeShared.Caching;

namespace PoeShared.Prism;

public sealed class LambdaFactory<TOut> : ICachingProxyFactory<TOut>
{
    private readonly Func<TOut> factoryFunc;

    public LambdaFactory(Func<TOut> factoryFunc)
    {
        this.factoryFunc = factoryFunc;
    }

    public TOut Create()
    {
        return factoryFunc();
    }
}