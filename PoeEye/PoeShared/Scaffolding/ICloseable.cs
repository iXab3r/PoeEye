using System.ComponentModel;

namespace PoeShared.Scaffolding;

/// <summary>
/// Provides a mechanism for objects that can be closed. This interface is typically used for objects
/// that require clean-up or release of resources when they are no longer needed.
/// It includes a property for accessing the close controller responsible for the closing logic.
/// </summary>
public interface ICloseable : INotifyPropertyChanged
{
    /// <summary>
    /// Gets or sets the controller responsible for closing the object.
    /// </summary>
    public ICloseController CloseController { get; set; }
}

/// <summary>
/// Provides a generic mechanism for objects that can be closed, where the type of the close controller
/// is specified. This interface is useful for objects that have type-specific closing logic or resources
/// that need to be released in a particular manner.
/// </summary>
/// <typeparam name="T">The type of the close controller.</typeparam>
public interface ICloseable<T> : INotifyPropertyChanged
{
    /// <summary>
    /// Gets or sets the controller responsible for closing the object of a specific type.
    /// </summary>
    public ICloseController<T> CloseController { get; set; }
}