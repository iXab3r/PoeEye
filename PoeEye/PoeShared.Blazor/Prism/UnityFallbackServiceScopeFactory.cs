using System;
using Microsoft.Extensions.DependencyInjection;
using Unity;

namespace PoeShared.Blazor.Prism;

public sealed class UnityFallbackServiceScopeFactory : IServiceScopeFactory
{
    private readonly IServiceProvider defaultProvider;
    private readonly IUnityContainer unityContainer;

    public UnityFallbackServiceScopeFactory(IServiceProvider defaultProvider, IUnityContainer unityContainer)
    {
        this.defaultProvider = defaultProvider;
        this.unityContainer = unityContainer;
    }

    public IServiceScope CreateScope()
    {
        return new UnityFallbackServiceScope(defaultProvider, unityContainer);
    }
}