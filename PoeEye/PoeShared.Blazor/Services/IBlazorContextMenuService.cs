#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace PoeShared.Blazor.Services;

public interface IBlazorContextMenuService
{
    IObservable<IList<CmItem>> WhenContextMenuRequested { get; }

    Task<IDisposable> RegisterAsync(ElementReference elementRef, Action<IList<CmItem>> handler);
}

public abstract class CmItem
{
    public string Label { get; init; } = "";
    public bool Enabled { get; init; } = true;
    /// <summary>
    /// You can keep a stable id for testing/telemetry if you want
    /// </summary>
    public string? Id { get; init; }
}

// Leaf types
public sealed class CmCommand : CmItem
{
    /// <summary>
    /// Return a small icon stream (e.g., 16x16 PNG). Factory so we don’t keep streams alive.
    /// </summary>
    public Func<Stream>? IconFactory { get; init; }

    /// <summary>
    /// Async handler for clicks
    /// </summary>
    public Func<CmInvokeContext, Task>? OnInvokeAsync { get; init; }
}

public sealed class CmCheckBox : CmItem
{
    public bool IsChecked { get; init; }
    
    public Func<CmInvokeContext, Task>? OnToggleAsync { get; init; }
}

public sealed class CmRadio : CmItem
{
    // Radios group by adjacency inside the same parent collection (matching WebView2 behavior).
    public bool IsChecked { get; init; }
    public Func<CmInvokeContext, Task>? OnSelectAsync { get; init; }
}

public sealed class CmSeparator : CmItem { }

public sealed class CmSubmenu : CmItem
{
    public System.Collections.Immutable.ImmutableArray<CmItem> Children { get; init; } = System.Collections.Immutable.ImmutableArray<CmItem>.Empty;
}

public sealed class CmInvokeContext
{
    public required string? ComponentId { get; init; }   // e.g., your data-cm-id, if any
    public required int X { get; init; }
    public required int Y { get; init; }
    public required object? Tag { get; init; }           // optional: your own payload
}