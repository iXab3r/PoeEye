using System.Collections.ObjectModel;
using DynamicData;
using ReactiveUI;

namespace PoeShared.Bindings;

/// <summary>
/// Represents an object that supports binding with reactive properties and objects.
/// </summary>
public interface IBindableReactiveObject : IDisposableReactiveObject
{
    /// <summary>
    /// Gets the unique identifier of the object that is unique amongst all BindableReactiveObjects for app lifetime
    /// </summary>
    string SessionObjectId { get; }

    /// <summary>
    /// Indicates whether the object has any bindings.
    /// </summary>
    bool HasBindings { get; }

    /// <summary>
    /// Gets a cache of reactive bindings, indexed by their target property paths.
    /// </summary>
    IObservableCache<IReactiveBinding, string> Bindings { get; }

    /// <summary>
    /// Removes a binding based on the target property path.
    /// </summary>
    /// <param name="targetPropertyPath">The path of the target property to unbind.</param>
    void RemoveBinding(string targetPropertyPath);

    /// <summary>
    /// Removes the specified binding from the object.
    /// </summary>
    /// <param name="binding">The binding to remove.</param>
    void RemoveBinding(IReactiveBinding binding);

    /// <summary>
    /// Adds or updates a binding.
    /// </summary>
    /// <param name="binding">The reactive binding to add or update.</param>
    void AddOrUpdateBinding(IReactiveBinding binding);

    /// <summary>
    /// Clears all bindings from the object.
    /// </summary>
    void ClearBindings();

    /// <summary>
    /// Adds or updates a binding between a target property and a source property of a given source object.
    /// </summary>
    /// <param name="targetPropertyPath">The path of the target property.</param>
    /// <param name="source">The source object.</param>
    /// <param name="sourcePropertyPath">The path of the source property.</param>
    /// <typeparam name="TSource">The type of the source object.</typeparam>
    /// <returns>The reactive binding created or updated.</returns>
    IReactiveBinding AddOrUpdateBinding<TSource>(string targetPropertyPath, TSource source, string sourcePropertyPath) where TSource : DisposableReactiveObject;

    /// <summary>
    /// Adds or updates a binding between a target property and a value provider.
    /// </summary>
    /// <param name="valueSource">The value provider.</param>
    /// <param name="targetPropertyPath">The path of the target property.</param>
    /// <returns>The reactive binding created or updated.</returns>
    IReactiveBinding AddOrUpdateBinding(IValueProvider valueSource, string targetPropertyPath);

    /// <summary>
    /// Resolves a binding for a given property path.
    /// </summary>
    /// <param name="propertyPath">The property path for which to resolve the binding.</param>
    /// <returns>The reactive binding associated with the specified property path.</returns>
    IReactiveBinding ResolveBinding(string propertyPath);
}
