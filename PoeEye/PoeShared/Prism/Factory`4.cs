
using Unity;
using Unity.Resolution;

namespace PoeShared.Prism;

internal sealed class Factory<TOut, TIn1, TIn2, TIn3> : IFactory<TOut, TIn1, TIn2, TIn3>, INamedFactory<TOut, TIn1, TIn2, TIn3>
{
    private readonly IUnityContainer container;

    public Factory(IUnityContainer container)
    {
        Guard.ArgumentNotNull(container, nameof(container));

        this.container = container;
    }

    public TOut Create(TIn1 param1, TIn2 param2, TIn3 param3)
    {
        return container.Resolve<TOut>(
            new DependencyOverride<TIn1>(param1),
            new DependencyOverride<TIn2>(param2),
            new DependencyOverride<TIn3>(param3));
    }

    public TOut Create(string name, TIn1 param1, TIn2 param2, TIn3 param3)
    {
        return container.Resolve<TOut>(
            name,
            new DependencyOverride<TIn1>(param1),
            new DependencyOverride<TIn2>(param2),
            new DependencyOverride<TIn3>(param3));
    }
}