using System.ComponentModel;
using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

/// <summary>
/// Represents an object that supports both disposability and reactive property changes.
/// </summary>
public interface IDisposableReactiveObject : IDisposable, INotifyPropertyChanged
{
    /// <summary>
    /// Gets a composite disposable container that can contain multiple disposables, 
    /// all of which will be disposed when this container is disposed.
    /// </summary>
    /// <returns>A composite disposable containing all disposables associated with this object.</returns>
    CompositeDisposable Anchors { [NotNull] get; }

    /// <summary>
    /// Raises a change notification indicating that a property has changed.
    /// </summary>
    /// <param name="propertyName">The name of the property that has changed.</param>
    void RaisePropertyChanged([CanBeNull] string propertyName);
}
