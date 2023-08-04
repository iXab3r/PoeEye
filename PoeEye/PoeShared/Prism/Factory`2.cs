
using Unity;
using Unity.Resolution;

namespace PoeShared.Prism;

internal sealed class Factory<TOut, TIn1> : IFactory<TOut, TIn1>, INamedFactory<TOut, TIn1>
{
    private readonly IUnityContainer container;

    public Factory(IUnityContainer container)
    {
        Guard.ArgumentNotNull(container, nameof(container));

        this.container = container;
    }

    public TOut Create(TIn1 param1)
    {
        return container.Resolve<TOut>(new DependencyOverride<TIn1>(param1));
    }

    public TOut Create(string name, TIn1 param1)
    {
        return container.Resolve<TOut>(name, new DependencyOverride<TIn1>(param1));
    }
}