using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace PoeShared.Blazor.Wpf;

public class BlazorWindowContentBase<T> : BlazorReactiveComponent<T> where T : class
{
    private static readonly IReadOnlyDictionary<string, object> EmptyAutomationAttributes = new Dictionary<string, object>(0);
    
    protected IReadOnlyDictionary<string, object> ComponentAutomationAttributes => CreateAutomationAttributes();
    
    /// <summary>
    /// Gets or sets user-defined stable automation id that can be surfaced as HTML metadata.
    /// </summary>
    [Parameter]
    public string AutomationId { get; set; }
    
    protected static void SetAutomationAttribute(IDictionary<string, object> attributes, string name, string value)
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
    
    
    protected IReadOnlyDictionary<string, object> CreateAutomationAttributes(string automationId = null, object dataContext = null)
    {
        automationId ??= AutomationId;
        dataContext ??= DataContext;

        Dictionary<string, object> attributes = null;
        if (!string.IsNullOrWhiteSpace(automationId))
        {
            attributes ??= new Dictionary<string, object>(StringComparer.Ordinal);
            attributes[WellKnownAutomationIds.AutomationIdAttribute] = automationId.Trim();
        }

        var dataContextTypeName = ResolveDataContextTypeName(dataContext);
        if (!string.IsNullOrWhiteSpace(dataContextTypeName))
        {
            attributes ??= new Dictionary<string, object>(StringComparer.Ordinal);
            attributes[WellKnownAutomationIds.DataContextTypeAttribute] = dataContextTypeName;
        }

        return attributes ?? EmptyAutomationAttributes;
    }
    
    protected static string ResolveDataContextTypeName(object dataContext)
    {
        return dataContext?.GetType().FullName ?? dataContext?.GetType().Name;
    }
}