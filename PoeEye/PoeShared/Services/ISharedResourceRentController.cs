using System.ComponentModel;

namespace PoeShared.Services;

/// <summary>
/// Manages the rental status of a shared resource, allowing multiple reasons for the resource to be rented.
/// </summary>
/// <example>
/// This example demonstrates how to use the <see cref="ISharedResourceRentController"/> interface to rent a resource.
/// <code>
/// var manager = new SharedResourceRentManager();
/// using (manager.Rent("Task A"))
/// {
///     // The resource is now rented for Task A.
///     // Do work with the rented resource.
/// }
/// // The resource is no longer rented for Task A.
/// </code>
/// </example>
public interface ISharedResourceRentController : INotifyPropertyChanged
{
    /// <summary>
    /// Provides optional name of the resource.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Provides whether the resource is rented.
    /// The key difference between this property and <see cref="WhenRented"/> is that this uses NotifyPropertyChanged to notify subscribers,
    /// which is known to have worse performance AND reliability than RX-based solutions.
    /// Use this property only when performance is not a concern, e.g. for IsLoading indicators
    /// </summary>
    bool IsRented { get; }
    
    /// <summary>
    /// Provides the current rental status of the resource.
    /// The key difference between this property and <see cref="WhenRented"/> is that this uses NotifyPropertyChanged to notify subscribers,
    /// which is known to have worse performance AND reliability than RX-based solutions.
    /// Use this property only when performance is not a concern, e.g. for IsLoading indicators 
    /// </summary>
    AnnotatedBoolean IsRentedState { get; }
    
    /// <summary>
    /// Provides the current rental status of the resource.
    /// If there are multiple reasons why the resource is rented, they will all be listed together.
    /// </summary>
    /// <example>
    /// This example demonstrates how to subscribe to the rental status of the resource.
    /// <code>
    /// var manager = new SharedResourceRentManager();
    /// manager.IsRented.Subscribe(status =>
    /// {
    ///     if (status.Value)
    ///     {
    ///         Console.WriteLine("Resource is rented for the following reasons: " + status.Annotation);
    ///     }
    ///     else
    ///     {
    ///         Console.WriteLine("Resource is not rented.");
    ///     }
    /// });
    /// </code>
    /// </example>
    IObservable<AnnotatedBoolean> WhenRented { get; }
        
    /// <summary>
    /// Temporarily rents the resource for the specified reason.
    /// </summary>
    /// <param name="reason">The reason for renting the resource.</param>
    /// <returns>An anchor which must be disposed as soon as the resource is no longer needed. Disposing the anchor releases the rental for the specified reason.</returns>
    /// <example>
    /// This example demonstrates how to rent and release the resource for a specific reason.
    /// <code>
    /// var manager = new SharedResourceRentManager();
    /// IDisposable rental = manager.Rent("Task B");
    /// 
    /// // The resource is now rented for Task B.
    /// 
    /// rental.Dispose();
    /// // The resource is no longer rented for Task B.
    /// </code>
    /// </example>
    IDisposable Rent(string reason);
}