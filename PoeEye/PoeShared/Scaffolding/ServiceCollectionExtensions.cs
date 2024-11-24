using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PoeShared.Prism;

namespace PoeShared.Scaffolding;

public static class ServiceCollectionExtensions
{
    public static void TryAdd<TServiceImplementation>(this IServiceCollection collection,
        ServiceLifetime lifetime)
    {
        TryAdd<TServiceImplementation, TServiceImplementation>(collection, lifetime);
    }

    public static void TryAdd(
        this IServiceCollection collection,
        Type serviceType,
        Type serviceImplementation,
        ServiceLifetime lifetime)
    {
        var descriptor = new ServiceDescriptor(serviceType, serviceImplementation, lifetime);
        collection.TryAdd(descriptor);
    }

    public static void TryAdd<TServiceType, TServiceImplementation>(
        this IServiceCollection collection,
        ServiceLifetime lifetime) where TServiceImplementation : TServiceType
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        TryAdd(collection, typeof(TServiceType), typeof(TServiceImplementation), lifetime);
    }
    
    public static void AddHostedServiceOfType<T>(this IServiceCollection services) where T : class
    {
        services.AddFactory<T>();
        services.AddHostedService<ServiceHosting<T>>();
    }

    public static void AddHostedService<TService, TImplementation>(this IServiceCollection services)
        where TService : class, IHostedService
        where TImplementation : class, TService
    {
        services.AddSingleton<TService, TImplementation>();
        services.AddHostedService<TService>(sp => sp.GetRequiredService<TService>());
    }

    public static void AddFactory<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.AddTransient<TService, TImplementation>();
        AddFactory<TService>(services);
    }
        
    public static void AddFactory<TService>(this IServiceCollection services)
        where TService : class
    {
        services.AddSingleton<Func<TService>>(x => x.GetRequiredService<TService>);
        services.AddSingleton<IFactory<TService>, Factory<TService>>();
    }

    internal sealed class Factory<T> : IFactory<T>
    {
        private readonly Func<T> factoryFunc;

        public Factory(Func<T> factoryFunc)
        {
            Guard.ArgumentNotNull(factoryFunc, nameof(factoryFunc));
            this.factoryFunc = factoryFunc;
        }

        public T Create()
        {
            return factoryFunc();
        }
    }
}