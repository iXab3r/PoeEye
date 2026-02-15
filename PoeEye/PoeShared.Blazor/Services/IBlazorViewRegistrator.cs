using System;
using System.Reflection;

namespace PoeShared.Blazor.Services;

/// <summary>
/// Represents a service for registering Blazor view types with associated content types and optional keys.
/// By using this service, developers can dynamically register views, making Blazor applications more extensible.
/// </summary>
/// <example>
/// <code>
/// // Registering a view:
/// var registrator = someServiceProvider.GetService&lt;IBlazorViewRegistrator&gt;();
/// registrator.RegisterViewType(typeof(MyCustomView));
/// </code>
/// </example>
public interface IBlazorViewRegistrator
{
    /// <summary>
    /// Registers all views that are present in the specified assembly.
    /// Once a view type is registered, it can be looked up using its content type and the specified key.
    /// </summary>
    /// <param name="assembly">Assembly containing Blazor views</param>
    /// <example>
    /// <code>
    /// registrator.RegisterViews(Assembly.GetExecutingAssembly());
    /// </code>
    /// </example>
    void RegisterViews(Assembly assembly);
    
    /// <summary>
    /// Registers the provided Blazor view type with an optional associated key.
    /// Once a view type is registered, it can be looked up using its content type and the specified key.
    /// </summary>
    /// <param name="viewType">The Blazor view type to register. This type should inherit from a base Blazor view component.</param>
    /// <param name="key">An optional key to associate with the view type. This allows multiple views to be registered for the same content type but differentiated by key.</param>
    /// <example>
    /// <code>
    /// registrator.RegisterViewType(typeof(MyDetailView), "Detail");
    /// registrator.RegisterViewType(typeof(MySummaryView), "Summary");
    /// </code>
    /// </example>
    void RegisterViewType(Type viewType, object key = default);

    /// <summary>
    /// Registers the provided Blazor view type with an optional associated key.
    /// Once a view type is registered, it can be looked up using its content type and the specified key.
    /// </summary>
    /// <param name="viewType">The Blazor view type to register. This type should inherit from a base Blazor view component.</param>
    /// <param name="dataContextType">DataContext type that will be used to resolve the view when needed</param>
    /// <param name="key">An optional key to associate with the view type. This allows multiple views to be registered for the same content type but differentiated by key.</param>
    /// <example>
    /// <code>
    /// registrator.RegisterViewType(typeof(MyDetailView), typeof(MyDetailViewModel), "Detail");
    /// registrator.RegisterViewType(typeof(MySummaryView), typeof(MySummaryViewModel), "Summary");
    /// </code>
    /// </example>
    void RegisterViewType(Type viewType, Type dataContextType, object key = default);
}