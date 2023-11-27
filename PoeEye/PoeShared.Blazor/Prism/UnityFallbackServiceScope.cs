using System;
using Microsoft.Extensions.DependencyInjection;
using Unity;

namespace PoeShared.Blazor.Prism;

public sealed class UnityFallbackServiceScope : IServiceScope
{
    private readonly IServiceProvider defaultProvider;
    private readonly IServiceScope defaultProviderScope;
    private readonly IUnityContainer unityContainer;
    private readonly IUnityContainer childUnityContainer;
    
    public UnityFallbackServiceScope(IServiceProvider defaultProvider, IUnityContainer unityContainer)
    {
        this.defaultProvider = defaultProvider;
        this.unityContainer = unityContainer;
        
        childUnityContainer = unityContainer.CreateChildContainer();
        defaultProviderScope = defaultProvider.CreateScope();
        ServiceProvider = new UnityFallbackServiceProvider(defaultProviderScope.ServiceProvider, childUnityContainer);
    }

    public void Dispose()
    {
        childUnityContainer.Dispose();
        defaultProviderScope.Dispose();
    }

    public IServiceProvider ServiceProvider { get; }
}