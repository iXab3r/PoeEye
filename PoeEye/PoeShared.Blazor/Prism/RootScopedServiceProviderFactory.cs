using System;
using Microsoft.Extensions.DependencyInjection;
using Unity;

namespace PoeShared.Blazor.Prism;

public sealed class RootScopedServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
    private readonly DefaultServiceProviderFactory defaultServiceProviderFactory;
    
    public RootScopedServiceProviderFactory()
    {
        defaultServiceProviderFactory = new DefaultServiceProviderFactory();
    }

    public IServiceCollection CreateBuilder(IServiceCollection services)
    {
        return services;
    }

    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
    {
        var serviceProvider = defaultServiceProviderFactory.CreateServiceProvider(containerBuilder);
        var rootScope = serviceProvider.CreateScope();
        return rootScope.ServiceProvider;
    }
}