using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using PoeShared.Blazor;
using PoeShared.Blazor.Wpf;

namespace PoeShared.Blazor.Avalonia;

public abstract class AvaloniaBlazorWindowComponentBase : ComponentBase, IDisposable
{
    private static readonly IReadOnlyDictionary<string, object> EmptyAutomationAttributes = new Dictionary<string, object>(0);
    private INotifyPropertyChanged? subscribedWindow;

    [Parameter]
    public IBlazorWindow DataContext { get; set; } = default!;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        var nextWindow = DataContext as INotifyPropertyChanged;
        if (ReferenceEquals(subscribedWindow, nextWindow))
        {
            return;
        }

        if (subscribedWindow != null)
        {
            subscribedWindow.PropertyChanged -= OnWindowPropertyChanged;
        }

        subscribedWindow = nextWindow;
        if (subscribedWindow != null)
        {
            subscribedWindow.PropertyChanged += OnWindowPropertyChanged;
        }
    }

    public virtual void Dispose()
    {
        if (subscribedWindow != null)
        {
            subscribedWindow.PropertyChanged -= OnWindowPropertyChanged;
            subscribedWindow = null;
        }
    }

    protected static Dictionary<string, object?> BuildComponentParameters(
        Type? componentType,
        object? content,
        IReadOnlyDictionary<string, object?>? additionalParameters = null)
    {
        var parameters = additionalParameters != null
            ? new Dictionary<string, object?>(additionalParameters)
            : new Dictionary<string, object?>();

        if (componentType?.GetProperty(nameof(DataContext), BindingFlags.Public | BindingFlags.Instance) != null &&
            !parameters.ContainsKey(nameof(DataContext)))
        {
            parameters[nameof(DataContext)] = content;
        }

        return parameters;
    }

    protected static void SetAutomationAttribute(IDictionary<string, object> attributes, string name, string? value)
    {
        if (attributes == null)
        {
            throw new ArgumentNullException(nameof(attributes));
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        attributes[name] = value.Trim();
    }

    protected IReadOnlyDictionary<string, object> CreateAutomationAttributes(string? automationId = null, object? dataContext = null)
    {
        automationId ??= GetWindowAutomationId();
        dataContext ??= DataContext?.DataContext;

        Dictionary<string, object>? attributes = null;
        if (!string.IsNullOrWhiteSpace(automationId))
        {
            attributes ??= new Dictionary<string, object>(StringComparer.Ordinal);
            attributes[WellKnownAutomationIds.AutomationIdAttribute] = automationId.Trim();
        }

        var dataContextTypeName = dataContext?.GetType().FullName ?? dataContext?.GetType().Name;
        if (!string.IsNullOrWhiteSpace(dataContextTypeName))
        {
            attributes ??= new Dictionary<string, object>(StringComparer.Ordinal);
            attributes[WellKnownAutomationIds.DataContextTypeAttribute] = dataContextTypeName;
        }

        return attributes ?? EmptyAutomationAttributes;
    }

    protected string? GetWindowAutomationId()
    {
        return DataContext is AvaloniaBlazorWindow window && !string.IsNullOrWhiteSpace(window.AutomationId)
            ? window.AutomationId
            : null;
    }

    protected string? GetViewAutomationId(string viewRole)
    {
        var windowAutomationId = GetWindowAutomationId();
        return string.IsNullOrWhiteSpace(windowAutomationId) || string.IsNullOrWhiteSpace(viewRole)
            ? null
            : $"{windowAutomationId}/{viewRole}";
    }

    private void OnWindowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }
}
