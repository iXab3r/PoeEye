using System;

namespace PoeShared.Prism;

public sealed class LambdaFactory<TOut> : IFactory<TOut>
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