using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PoeShared.Prism;

namespace PoeShared.Scaffolding;

public static class ServiceCollectionExtensions
{
    private static readonly IFluentLog Log = typeof(ServiceCollectionExtensions).PrepareLogger();

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
    
    public static void AddFactories(this IServiceCollection services)
    {
        services.Add(new ServiceDescriptor(typeof(IFactory<>),  typeof(LifetimeFactory<>), ServiceLifetime.Singleton));
        services.Add(new ServiceDescriptor(typeof(IScopedFactory<>),  typeof(ScopedFactory<>), ServiceLifetime.Scoped));
    }

    public static void AddFactory<TImplementation>(this IServiceCollection services, Func<IServiceProvider, TImplementation> factoryFunc)
        where TImplementation : class
    {
        EnsureNotAbstract<TImplementation>();
        
        services.Add(new ServiceDescriptor(typeof(IFactory<TImplementation>), x =>  new LifetimeFactory<TImplementation>(x, factoryFunc), ServiceLifetime.Singleton));
        services.Add(new ServiceDescriptor(typeof(IScopedFactory<TImplementation>), x =>  new ScopedFactory<TImplementation>(x, factoryFunc), ServiceLifetime.Scoped));
    }

    public static void AddFactory<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        EnsureNotAbstract<TImplementation>();
        
        services.Add(new ServiceDescriptor(typeof(IFactory<TService>), x =>  new LifetimeFactory<TImplementation>(x), ServiceLifetime.Singleton));
        services.Add(new ServiceDescriptor(typeof(IScopedFactory<TService>), x =>  new ScopedFactory<TImplementation>(x), ServiceLifetime.Scoped));
        services.Add(new ServiceDescriptor(typeof(IFactory<TImplementation>), x =>  new LifetimeFactory<TImplementation>(x), ServiceLifetime.Singleton));
        services.Add(new ServiceDescriptor(typeof(IScopedFactory<TImplementation>), x =>  new ScopedFactory<TImplementation>(x), ServiceLifetime.Scoped));
    }

    internal sealed class ScopedFactory<T> : IScopedFactory<T>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly Func<IServiceProvider, T> factoryFunc;
        
        public ScopedFactory(IServiceProvider serviceProvider) : this(serviceProvider, CreateInstance<T>)
        {
        }

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
    }
    
    internal sealed class LifetimeFactory<T> : IFactory<T>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly Func<IServiceProvider, T> factoryFunc;
        private readonly IServiceScope serviceScope;
        
        public LifetimeFactory(IServiceProvider serviceProvider) : this(serviceProvider, CreateInstance<T>)
        {
        }

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

    private static void EnsureNotAbstract<TService>()
    {
        var type = typeof(TService);
        if (type.IsInterface)
        {
            throw new ArgumentException($"Failed to to register factory for interface {typeof(TService)}");
        }
        if (type.IsAbstract)
        {
            throw new ArgumentException($"Failed to to register factory for abstract class {typeof(TService)}");
        }
    }
    
    private static TService CreateInstance<TService>(IServiceProvider serviceProvider)
    {
        try
        {
            return ActivatorUtilities.CreateInstance<TService>(serviceProvider);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to resolve instance of type {typeof(TService)}, service provider: {serviceProvider}", e);
            throw;
        }
    }
}