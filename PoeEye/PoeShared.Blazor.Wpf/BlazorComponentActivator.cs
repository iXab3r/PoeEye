using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace PoeShared.Blazor.Wpf;

internal sealed class BlazorComponentActivator : IComponentActivator
{
    private readonly IServiceProvider serviceProvider;

    public BlazorComponentActivator(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
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
        
        return (IComponent) Activator.CreateInstance(componentType);
    }
}