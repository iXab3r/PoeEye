using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Components;

namespace PoeShared.Blazor.Controls.Services;

/// <summary>
/// Stores parameters for dynamically rendered Blazor components and returns an opaque <see cref="DynamicComponentId"/> key
/// that can later be used by a <see cref="DynamicComponentContainer"/> to render the component via
/// <see cref="Microsoft.AspNetCore.Components.DynamicComponent"/>.
/// </summary>
/// <remarks>
/// Typical usage:
/// <para>
/// 1) Register a component and its parameters in code-behind or service layer and keep the returned id:
/// <code>
/// var id = storage.Register<MyComponent>(new Dictionary<string, object?>
/// {
///     [nameof(MyComponent.Title)] = "Hello"
/// });
/// </code>
/// 2) Pass the id to the view and render it using the DynamicComponentContainer:
/// <code>
/// &lt;DynamicComponentContainer Id="@id" /&gt;
/// </code>
/// </para>
/// The storage is thread-safe and intended to be registered as a singleton.
/// </remarks>
public interface IDynamicComponentParameterStorage
{
    /// <summary>
    /// Registers a dynamic component of type <typeparamref name="T"/> together with an optional set of parameters
    /// and returns a unique <see cref="DynamicComponentId"/> to reference it later.
    /// </summary>
    /// <typeparam name="T">Blazor component type to be rendered later, must inherit from <see cref="ComponentBase"/>.</typeparam>
    /// <param name="componentParameters">Optional parameter bag (parameter name to value) passed to the component when rendered.</param>
    /// <returns>An opaque identifier that can be supplied to <see cref="DynamicComponentContainer"/>.</returns>
    DynamicComponentId Register<T>(IDictionary<string, object?>? componentParameters = null) where T : ComponentBase;

    /// <summary>
    /// Unregisters a previously registered dynamic component by its <paramref name="dynamicComponentId"/>.
    /// After unregistration, the associated component parameters are removed from the storage and the id becomes invalid.
    /// </summary>
    /// <param name="dynamicComponentId">The identifier that was returned by <see cref="Register{T}(System.Collections.Generic.IDictionary{string, object})"/>.</param>
    /// <remarks>
    /// It is safe to call this method multiple times for the same id; subsequent calls will have no effect.
    /// Typical call sites include component disposal handlers (for example, <see cref="DynamicComponentContainer"/>)
    /// or code that programmatically removes dynamically created UI elements.
    /// </remarks>
    void Unregister(DynamicComponentId dynamicComponentId);

    /// <summary>
    /// Retrieves the registration for the specified <paramref name="dynamicComponentId"/>.
    /// </summary>
    /// <param name="dynamicComponentId">The identifier returned from <see cref="Register{T}(System.Collections.Generic.IDictionary{string, object})"/>.</param>
    /// <returns>The stored <see cref="DynamicComponentRegistration"/> containing component type and parameters.</returns>
    /// <remarks>
    /// This method is intended for internal use by <see cref="DynamicComponentContainer"/>.
    /// </remarks>
    DynamicComponentRegistration Get(DynamicComponentId dynamicComponentId);
}

internal sealed class DynamicComponentParameterStorage : IDynamicComponentParameterStorage
{
    private readonly ConcurrentDictionary<DynamicComponentId, DynamicComponentRegistration> registrationsById = new();

    private long globalComponentCounter;

    /// <inheritdoc />
    public DynamicComponentRegistration Get(DynamicComponentId dynamicComponentId)
    {
        return registrationsById[dynamicComponentId];
    }

    /// <inheritdoc />
    public DynamicComponentId Register<T>(IDictionary<string, object?>? componentParameters = null) where T : ComponentBase
    {
        var componentIndex = Interlocked.Increment(ref globalComponentCounter);
        var newRegistration = new DynamicComponentRegistration()
        {
            ComponentId = new DynamicComponentId($"dynamic-component-{componentIndex}"),
            ComponentParameters = componentParameters ?? new Dictionary<string, object?>(),
            ComponentType = typeof(T)
        };
        registrationsById[newRegistration.ComponentId] = newRegistration;
        return newRegistration.ComponentId;
    }

    /// <inheritdoc />
    public void Unregister(DynamicComponentId dynamicComponentId)
    {
        registrationsById.TryRemove(dynamicComponentId, out _);
    }
}

/// <summary>
/// Describes a dynamic component to be rendered: its concrete component type and the parameter bag
/// to be supplied when rendered via <see cref="Microsoft.AspNetCore.Components.DynamicComponent"/>.
/// </summary>
public readonly record struct DynamicComponentRegistration
{
    public DynamicComponentRegistration()
    {
    }
    
    public required DynamicComponentId ComponentId { get; init; }

    /// <summary>
    /// The Blazor component type to instantiate.
    /// </summary>
    public required Type ComponentType { get; init; }
    
    /// <summary>
    /// Parameter name/value map passed to the component instance.
    /// </summary>
    public required IDictionary<string, object?> ComponentParameters { get; init; }
}
