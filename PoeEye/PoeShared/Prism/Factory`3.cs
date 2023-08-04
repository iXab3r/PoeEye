
using Unity;
using Unity.Resolution;

namespace PoeShared.Prism;

internal sealed class Factory<TOut, TIn1, TIn2> : IFactory<TOut, TIn1, TIn2>, INamedFactory<TOut, TIn1, TIn2>
{
    private readonly IUnityContainer container;

    public Factory(IUnityContainer container)
    {
        Guard.ArgumentNotNull(container, nameof(container));

        this.container = container;
    }

    public TOut Create(TIn1 parameter1, TIn2 parameter2)
    {
        return container.Resolve<TOut>(new DependencyOverride<TIn1>(parameter1), new DependencyOverride<TIn2>(parameter2));
    }

    public TOut Create(string name, TIn1 param1, TIn2 param2)
    {
        return container.Resolve<TOut>(name, new DependencyOverride<TIn1>(param1), new DependencyOverride<TIn2>(param2));
    }
}