using System;
using Unity;

namespace PoeShared.Blazor.Prism;

/// <summary>
/// Represents a service provider that first attempts to resolve services using
/// the .NET Core's default dependency injection container, and if the service is not
/// found, it falls back to the Unity container.
/// </summary>
/// <remarks>
/// <para>
/// The .NET Core's dependency injection container and Unity's container have different
/// mechanisms for service registration and resolution. This service provider aims to
/// bridge the gap between the two, allowing for a unified service resolution strategy
/// that can work with both containers. However, it is important to be aware of the
/// differences in lifetime management and other features between the two containers.
/// </para>
/// <para>
/// Unity Container:
/// - Supports named registrations.
/// - Allows more complex lifetime management including external control.
/// - Provides property injection.
/// </para>
/// <para>
/// .NET Core DI Container:
/// - Does not support named registrations out of the box.
/// - Primarily supports three lifetimes - Singleton, Scoped, and Transient.
/// - Focuses on constructor injection.
/// </para>
/// <para>
/// It is crucial to handle these differences carefully, especially for services with
/// specific lifetime requirements or advanced configuration.
/// </para>
/// </remarks>
public sealed class UnityFallbackServiceProvider : IServiceProvider
{
    private readonly IServiceProvider defaultProvider;
    private readonly IUnityContainer unityContainer;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnityFallbackServiceProvider"/> class.
    /// </summary>
    /// <param name="defaultProvider">The default .NET Core service provider.</param>
    /// <param name="unityContainer">The Unity container.</param>
    public UnityFallbackServiceProvider(IServiceProvider defaultProvider, IUnityContainer unityContainer)
    {
        this.defaultProvider = defaultProvider ?? throw new ArgumentNullException(nameof(defaultProvider));
        this.unityContainer = unityContainer ?? throw new ArgumentNullException(nameof(unityContainer));
    }

    /// <summary>
    /// Gets the service of the specified type from the .NET Core service provider
    /// or falls back to the Unity container if the service is not found.
    /// </summary>
    /// <param name="serviceType">The type of the service to get.</param>
    /// <returns>The service instance or <c>null</c> if the service is not found.</returns>
    public object GetService(Type serviceType)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

        // Try to get the service from the default provider
        var service = defaultProvider.GetService(serviceType);

        // If not found, fall back to Unity container
        if (service == null)
        {
            service = unityContainer.Resolve(serviceType, null);
        }

        return service;
    }
}
