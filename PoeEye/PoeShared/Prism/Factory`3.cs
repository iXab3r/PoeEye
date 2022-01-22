﻿
using Unity;
using Unity.Resolution;

namespace PoeShared.Prism;

internal sealed class Factory<T1, T2, T3> : IFactory<T1, T2, T3>, INamedFactory<T1, T2, T3>
{
    private readonly IUnityContainer container;

    public Factory(IUnityContainer container)
    {
        Guard.ArgumentNotNull(container, nameof(container));

        this.container = container;
    }

    public T1 Create(T2 parameter1, T3 parameter2)
    {
        return container.Resolve<T1>(new DependencyOverride<T2>(parameter1), new DependencyOverride<T3>(parameter2));
    }

    public T1 Create(string name, T2 param1, T3 param2)
    {
        return container.Resolve<T1>(name, new DependencyOverride<T2>(param1), new DependencyOverride<T3>(param2));
    }
}