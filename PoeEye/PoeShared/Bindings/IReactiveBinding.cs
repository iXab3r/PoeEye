namespace PoeShared.Bindings;

/// <summary>
/// Defines a reactive binding that connects a source and a target, ensuring the target reflects changes in the source.
/// </summary>
public interface IReactiveBinding : IDisposableReactiveObject
{
    /// <summary>
    /// Gets the property path of the target. This can be a nested path representing where the source's value will be applied.
    /// </summary>
    /// <value>
    /// A string representing the property path of the target.
    /// </value>
    string TargetPropertyPath { get; }
        
    /// <summary>
    /// Gets a string representing any error that occurs during the binding process.
    /// </summary>
    /// <value>
    /// The error message, if any; otherwise, null.
    /// </value>
    string Error { get; }
        
    /// <summary>
    /// Gets the source watcher that monitors changes in the source's value.
    /// </summary>
    /// <value>
    /// An <see cref="IValueProvider"/> that watches the source.
    /// </value>
    IValueProvider SourceWatcher { get; }
        
    /// <summary>
    /// Gets the target watcher that receives and applies changes from the source.
    /// </summary>
    /// <value>
    /// An <see cref="IValueWatcher"/> that watches and updates the target.
    /// </value>
    IValueWatcher TargetWatcher { get; }
        
    /// <summary>
    /// Gets a value indicating whether the binding is currently active and functioning.
    /// </summary>
    /// <value>
    /// True if the binding is active; otherwise, false.
    /// </value>
    bool IsActive { get; }
}
