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
    
    public static void AddHostedServiceOfType<TService>(this IServiceCollection services) where TService : class
    {
        services.AddHostedService<ServiceHosting<TService>>();
    }

    public static void AddFactory<TService>(this IServiceCollection services, Func<IServiceProvider, TService> factoryFunc)
        where TService : class
    {
        services.Add(new ServiceDescriptor(typeof(IFactory<TService>), x =>  new LifetimeFactory<TService>(x, factoryFunc), ServiceLifetime.Singleton));
        services.Add(new ServiceDescriptor(typeof(IScopedFactory<TService>), x =>  new ScopedFactory<TService>(x, factoryFunc), ServiceLifetime.Scoped));
    }

    public static void AddFactory<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        AddFactory<TService>(services, x => ActivatorUtilities.CreateInstance<TImplementation>(x));
    }

    public static void AddFactory<TService>(this IServiceCollection services)
        where TService : class
    {
        AddFactory(services, x => ActivatorUtilities.CreateInstance<TService>(x));
    }

    internal sealed class ScopedFactory<T> : IScopedFactory<T>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly Func<IServiceProvider, T> factoryFunc;
        private readonly CompositeDisposable anchors = new CompositeDisposable();

        public ScopedFactory(IServiceProvider serviceProvider, Func<IServiceProvider, T> factoryFunc)
        {
            this.serviceProvider = serviceProvider;
            this.factoryFunc = factoryFunc;
        }

        public T Create()
        {
            var scope = serviceProvider.CreateScope();
            return factoryFunc(scope.ServiceProvider);
        }

        public void Dispose()
        {
            if (anchors.IsDisposed)
            {
                return;
            }
            anchors.Dispose();
        }
    }
    
    internal sealed class LifetimeFactory<T> : IFactory<T>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly Func<IServiceProvider, T> factoryFunc;
        private readonly IServiceScope serviceScope;

        public LifetimeFactory(IServiceProvider serviceProvider, Func<IServiceProvider, T> factoryFunc)
        {
            this.serviceProvider = serviceProvider;
            this.factoryFunc = factoryFunc;
            serviceScope = this.serviceProvider.CreateScope();
        }

        public T Create()
        {
            return factoryFunc(serviceScope.ServiceProvider);
        }
    }
}