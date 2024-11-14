using System;
using System.Collections.Immutable;
using System.Windows;
using Microsoft.Extensions.FileProviders;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Defines the contract for a Blazor window with configurable UI properties, event-driven lifecycle, and window state operations.
/// </summary>
public interface IBlazorWindow : IBlazorWindowController, IDisposableReactiveObject, IBlazorWindowNativeController
{
    /// <summary>
    /// Gets or sets type of View which will be displayed within the window.
    /// </summary>
    Type ViewType { get; set; }
    
    /// <summary>
    /// Gets or sets the data context which will be assigned to View.
    /// </summary>
    object ViewDataContext { get; set; }
    
    /// <summary>
    /// Gets or sets list of additional files which will be included into browser
    /// </summary>
    ImmutableArray<IFileInfo> AdditionalFiles { get; set; }

    /// <summary>
    /// Gets or sets the startup location of the window.
    /// </summary>
    WindowStartupLocation WindowStartupLocation { get; set; }
}