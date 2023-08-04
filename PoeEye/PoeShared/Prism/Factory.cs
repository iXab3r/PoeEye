
using PoeShared.Caching;
using Unity;

namespace PoeShared.Prism;

internal sealed class Factory<TOut> : INamedFactory<TOut>, ICachingProxyFactory<TOut>
{
    private readonly IUnityContainer container;

    public Factory(IUnityContainer container)
    {
        Guard.ArgumentNotNull(container, nameof(container));

        this.container = container;
    }

    public TOut Create()
    {
        return container.Resolve<TOut>();
    }

    public TOut Create(string name)
    {
        return container.Resolve<TOut>(name);
    }
}