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
        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", nameof(componentType));
        }

        if (typeof(BlazorReactiveComponent).IsAssignableFrom(componentType))
        {
            return (IComponent)serviceProvider.GetService(componentType);
        }
        else
        {
            return (IComponent) Activator.CreateInstance(componentType);
        }
    }
}