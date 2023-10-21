using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Unity;

namespace PoeShared.Blazor.Wpf;

internal sealed class BlazorComponentActivator : IComponentActivator
{
    private readonly IServiceProvider serviceProvider;
    private readonly IUnityContainer unityContainer;

    public BlazorComponentActivator(IServiceProvider serviceProvider, IUnityContainer unityContainer)
    {
        this.serviceProvider = serviceProvider;
        this.unityContainer = unityContainer;
    }
    
    /// <inheritdoc />
    public IComponent CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
    {
        if (componentType == null)
        {
            throw new ArgumentNullException(nameof(componentType));
        }

        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", nameof(componentType));
        }

        var result = (IComponent)serviceProvider.GetService(componentType);
        if (result != null)
        {
            return result;
        }

        if (unityContainer != null)
        {
            return (IComponent) unityContainer.Resolve(componentType);
        }
        
        return (IComponent) Activator.CreateInstance(componentType);
    }
}