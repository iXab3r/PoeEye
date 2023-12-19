namespace PoeShared.Common;

/// <summary>
/// Defines an interface for objects that can be loaded into memory or a specific context.
/// Can be used to specify that the object can be unloaded, saving some CPU/RAM resources.
/// </summary>
public interface IHasLoaded
{
    /// <summary>
    /// Gets or sets a value indicating whether the object is currently loaded or HAS to be loaded.
    /// </summary>
    bool IsLoaded { get; }
}