
using Unity;
using Unity.Resolution;

namespace PoeShared.Prism;

internal sealed class Factory<TOut, TIn1, TIn2, TIn3, TIn4, TIn5> : IFactory<TOut, TIn1, TIn2, TIn3, TIn4, TIn5>
{
    private readonly IUnityContainer container;

    public Factory(IUnityContainer container)
    {
        Guard.ArgumentNotNull(container, nameof(container));

        this.container = container;
    }

    public TOut Create(TIn1 param1, TIn2 param2, TIn3 param3, TIn4 param4, TIn5 param5)
    {
        return container.Resolve<TOut>(
            new DependencyOverride<TIn1>(param1),
            new DependencyOverride<TIn2>(param2),
            new DependencyOverride<TIn3>(param3),
            new DependencyOverride<TIn4>(param4),
            new DependencyOverride<TIn5>(param5));
    }
}