#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace PoeShared.Blazor.Services;

public interface IBlazorContextMenuService
{
    IObservable<IList<BlazorContextMenuItem>> WhenContextMenuRequested { get; }

    Task<IDisposable> RegisterAsync(ElementReference elementRef, Action<IList<BlazorContextMenuItem>> handler);

    Task ShowContextMenu(IList<BlazorContextMenuItem> items);
}

public abstract class BlazorContextMenuItem
{
    public string Label { get; init; } = "";
    
    public bool Enabled { get; init; } = true;
    /// <summary>
    /// You can keep a stable id for testing/telemetry if you want
    /// </summary>
    public string? Id { get; init; }
}

// Leaf types
public sealed class BlazorContextMenuCommand : BlazorContextMenuItem
{
    /// <summary>
    /// Returns and object which is supposed to be used for icon, could be Stream for image, could be string for font icon, etc.
    /// </summary>
    public Func<object>? IconFactory { get; init; }

    /// <summary>
    /// Async handler for clicks
    /// </summary>
    public Func<BlazorContextMenuInvokeContext, Task>? OnInvokeAsync { get; init; }
}

public sealed class BlazorContextMenuCheckBox : BlazorContextMenuItem
{
    public bool IsChecked { get; init; }
    
    public Func<BlazorContextMenuInvokeContext, Task>? OnToggleAsync { get; init; }
}

public sealed class BlazorContextMenuRadio : BlazorContextMenuItem
{
    // Radios group by adjacency inside the same parent collection (matching WebView2 behavior).
    public bool IsChecked { get; init; }
    public Func<BlazorContextMenuInvokeContext, Task>? OnSelectAsync { get; init; }
}

public sealed class BlazorContextMenuSeparator : BlazorContextMenuItem { }

public sealed class CmSubmenu : BlazorContextMenuItem
{
    public System.Collections.Immutable.ImmutableArray<BlazorContextMenuItem> Children { get; init; } = System.Collections.Immutable.ImmutableArray<BlazorContextMenuItem>.Empty;
}

public sealed class BlazorContextMenuInvokeContext
{
    public string? ComponentId { get; init; }   // e.g., your data-cm-id, if any
    
    /// <summary>
    /// Screen coordinates (not DPI-aware aka NOT device pixels)   
    /// </summary>
    public double X { get; init; }
    
    /// <summary>
    /// Screen coordinates (not DPI-aware aka NOT device pixels)
    /// </summary>
    public double Y { get; init; }
    
    public object? Tag { get; init; }           // optional: your own payload
}