namespace PoeShared.Bindings;

/// <summary>
/// Values providers are responsible for providing an actual value or providing info about an exception if value retrieval is not possible.
/// </summary>
public interface IValueProvider : IDisposableReactiveObject
{
    /// <summary>
    /// Gets a value indicating whether this provider has a value.
    /// </summary>
    /// <value>
    /// True if this provider has a value; otherwise, false.
    /// </value>
    bool HasValue { get; }
        
    /// <summary>
    /// Gets the value provided by this provider. Does not throw.
    /// </summary>
    /// <value>
    /// The provided value.
    /// </value>
    object Value { get; }
        
    /// <summary>
    /// Gets the exception representing the error that occurred during value retrieval.
    /// </summary>
    /// <value>
    /// The exception if an error occurs; otherwise, null.
    /// </value>
    Exception Error { get; }
}
