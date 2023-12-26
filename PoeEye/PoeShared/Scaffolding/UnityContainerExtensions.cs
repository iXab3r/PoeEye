using Microsoft.Extensions.DependencyInjection;
using PoeShared.Modularity;
using Unity;
using Unity.Extension;
using Unity.Lifetime;

namespace PoeShared.Scaffolding;

public static class UnityContainerExtensions
{
    private static readonly IFluentLog Log = typeof(UnityContainerExtensions).PrepareLogger();

    public static IUnityContainer ReplaceConfigMetadata<T>(this IUnityContainer container, string sourceTypeName)
    {
        var migrationService = container.Resolve<IPoeConfigMetadataReplacementService>();
        migrationService.AddMetadataReplacement(sourceTypeName, typeof(T));
        return container;
    }

    public static IUnityContainer RegisterSingleton<TTo>(this IUnityContainer instance, params Type[] types)
    {
        instance.RegisterSingleton(typeof(TTo));

        foreach (var type in types)
        {
            instance.RegisterType(type, typeof(TTo), new ContainerControlledLifetimeManager());
        }

        return instance;
    }

    public static IUnityContainer RegisterType<TTo>(this IUnityContainer instance, params Type[] types)
    {
        foreach (var type in types)
        {
            instance.RegisterType(type, typeof(TTo));
        }

        return instance;
    }

    public static IUnityContainer RegisterSingleton<TTo>(this IUnityContainer instance, Func<IUnityContainer, object> func)
    {
        return instance.RegisterFactory<TTo>(func, new ContainerControlledLifetimeManager());
    }

    public static IUnityContainer RegisterSingleton<TTo>(this IUnityContainer instance, string name, Func<IUnityContainer, object> func)
    {
        return instance.RegisterFactory<TTo>(name, func, new ContainerControlledLifetimeManager());
    }

    public static IUnityContainer AddNewExtensionIfNotExists<TExtension>(this IUnityContainer container)
        where TExtension : UnityContainerExtension
    {
        if (container.Configure<TExtension>() != null)
        {
            Log.Warn($"Extension of type {typeof(TExtension)} is already added - ignoring request");
            return container;
        }

        Log.Debug($"Adding new extension of type {typeof(TExtension)} to container, registered types: {container.Registrations.Count()}");
        return container.AddNewExtension<TExtension>();
    }

    /// <summary>
    /// Converts Unity container registrations to an enumerable of ServiceDescriptors.
    /// This allows for integrating Unity registrations with .NET Core's built-in dependency injection system.
    /// </summary>
    /// <param name="container">The Unity container with configured registrations.</param>
    /// <returns>An IEnumerable of ServiceDescriptor objects representing the Unity container registrations.</returns>
    /// <remarks>
    /// <para>
    /// Differences between Unity and .NET Core DI:
    /// - Unity supports more complex lifetime management and named registrations, 
    ///   which might not have direct equivalents in .NET Core DI.
    /// - .NET Core DI primarily focuses on constructor injection, though it supports property injection via [Inject] attribute
    /// - .NET Core DI does not support resolving unregistered concrete types by default, 
    ///   a feature that Unity provides.
    /// </para>
    /// <para>
    /// ServiceDescriptor represents a service registration in .NET Core DI. It encapsulates 
    /// the service type, its implementation, and its lifetime (Singleton, Scoped, or Transient).
    /// </para>
    /// <para>
    /// Possible Pitfalls:
    /// - Generic type registrations might need special handling, which is not covered by this method.
    /// - Specific lifetime managers in Unity (like ExternallyControlledLifetimeManager) 
    ///   do not have direct equivalents in .NET Core DI, and their behavior might differ after conversion.
    /// - Named registrations in Unity cannot be directly converted since .NET Core DI does not 
    ///   support named services.
    /// </para>
    /// </remarks>
    public static IEnumerable<ServiceDescriptor> ToServiceDescriptors(this IUnityContainer container)
    {
        if (container == null)
        {
            throw new ArgumentNullException(nameof(container));
        }

        foreach (var x in container.Registrations)
        {
            // Handling open generic types
            if (x.RegisteredType.IsGenericTypeDefinition)
            {
                if (x.MappedToType.IsGenericTypeDefinition)
                {
                    yield return new ServiceDescriptor(
                        x.RegisteredType,
                        x.MappedToType,
                        ConvertToServiceLifetime(x.LifetimeManager));
                }
                else
                {
                    Log.Warn($"Mismatched generic type mapping: {new {x.RegisteredType, x.MappedToType}}");
                    continue;
                }
            }
            else
            {
                // Handling non-generic and closed generic types
                var lifetime = ConvertToServiceLifetime(x.LifetimeManager);
                yield return new ServiceDescriptor(x.RegisteredType, sp => container.Resolve(x.RegisteredType), lifetime);
            }
        }
    }

    private static ServiceLifetime ConvertToServiceLifetime(LifetimeManager lifetimeManager)
    {
        return lifetimeManager switch
        {
            ContainerControlledLifetimeManager => ServiceLifetime.Singleton,
            _ => ServiceLifetime.Transient 
        };
    }

}