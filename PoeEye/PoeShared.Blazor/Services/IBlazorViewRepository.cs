using System;
using System.Reactive;

namespace PoeShared.Blazor.Services;

/// <summary>
/// Represents a repository for resolving Blazor view types based on content types and optional keys.
/// This interface is designed to facilitate dynamic view lookup, enhancing the modularity of a Blazor application.
/// </summary>
/// <example>
/// <code>
/// // Resolving a view by its content type:
/// var repo = someServiceProvider.GetService&lt;IBlazorViewRepository&gt;();
/// var viewType = repo.ResolveViewType(typeof(MyContentModel));
/// </code>
/// </example>
public interface IBlazorViewRepository
{
    /// <summary>
    /// Resolves and retrieves the registered Blazor view type for a given content type and an optional key.
    /// </summary>
    /// <param name="contentType">The content type associated with the desired Blazor view. This typically corresponds to a data model or ViewModel type that the view is designed to display.</param>
    /// <param name="key">An optional key that was used during view registration. If multiple views are registered for the same content type, the key can be used to differentiate between them.</param>
    /// <returns>The registered Blazor view type that matches the provided content type and key. If no match is found, returns null.</returns>
    /// <example>
    /// <code>
    /// var detailViewType = repo.ResolveViewType(typeof(MyContentModel), "Detail");
    /// var summaryViewType = repo.ResolveViewType(typeof(MyContentModel), "Summary");
    /// </code>
    /// </example>
    Type ResolveViewType(Type contentType, object key = default);
    
    IObservable<Unit> WhenChanged { get; }
}