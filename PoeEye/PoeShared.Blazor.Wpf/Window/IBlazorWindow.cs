using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.FileProviders;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using Unity;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Defines the contract for a Blazor window with configurable UI properties, event-driven lifecycle, and window state operations.
/// </summary>
public interface IBlazorWindow : IBlazorWindowController, IDisposableReactiveObject, IBlazorWindowNativeController
{
    public const string TitleBarAnchorName = $"--blazor-window-titlebar";
    public const string WindowAnchorName = $"--blazor-window";
    public const string WindowId = $"blazor-window";
    public const string WindowTitleBarId = $"blazor-window-titlebar";
    public const string WindowBody = $"blazor-window-body";
    public const string WindowStatusBarId = $"blazor-window-statusbar";
    
    /// <summary>
    /// Gets or sets type of View which will be displayed within the window.
    /// </summary>
    Type ViewType { get; set; }
    
    /// <summary>
    /// Gets or sets type of View which will be displayed inside title bar
    /// </summary>
    Type ViewTypeForTitleBar { get; set; }
    
    /// <summary>
    /// Gets or sets the data context which will be assigned to View.
    /// This property is a legacy one, will be removed in future versions and is replaced with DataContext
    /// </summary>
    object DataContext { get; set; }
    
    /// <summary>
    /// Gets or sets the data context which will be assigned to View.
    /// </summary>
    [Obsolete($"Replaced with {nameof(DataContext)} - to be removed in future versions")]
    [Browsable(false)]
    object ViewDataContext { get; set; }
    
    /// <summary>
    /// Gets or sets container which will be used by window to resolve the View
    /// </summary>
    IUnityContainer Container { get; set; }
    
    /// <summary>
    /// Gets or sets list of additional files which will be included into browser
    /// </summary>
    ImmutableArray<IFileInfo> AdditionalFiles { get; set; }
        
    /// <summary>
    /// Gets or sets additional file provider which will be used by Blazor
    /// </summary>
    IFileProvider AdditionalFileProvider { get; set; }
    
    /// <summary>
    /// Gets or sets Blazor control configurator which allows to inject custom actions into the pipeline
    /// </summary>
    IBlazorContentControlConfigurator ControlConfigurator { get; set; }

    /// <summary>
    /// Adds additional file provider which will be used by Blazor. Added to the end of the list.
    /// Removed on disposal
    /// </summary>
    IDisposable RegisterFileProvider(IFileProvider fileProvider);

    /// <summary>
    /// Shows Chromium DevTools
    /// </summary>
    void ShowDevTools();
}