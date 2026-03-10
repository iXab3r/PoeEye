using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components.Web;

namespace PoeShared.Blazor.Wpf.Scaffolding;

/// <summary>
/// This class is needed to enumerate over internal list of JS Custom components. Officially there is no way to get the list of components which were registered
/// Should be removed when MS will provide an official way
/// </summary>
internal sealed class JSComponentConfigurationStoreAccessor : IJSComponentConfiguration
{
    private readonly Dictionary<string, Type> jsComponentTypesByIdentifier;

    private static readonly FieldInfo JSComponentTypesByIdentifierField;

    static JSComponentConfigurationStoreAccessor()
    {
        JSComponentTypesByIdentifierField = typeof(JSComponentConfigurationStore)
                                                .GetField("_jsComponentTypesByIdentifier", BindingFlags.NonPublic | BindingFlags.Instance)
                                            ?? throw new ApplicationException($"Failed to get field _jsComponentTypesByIdentifier on type {typeof(JSComponentConfigurationStore)}");
    }

    public JSComponentConfigurationStoreAccessor(JSComponentConfigurationStore instance)
    {
        JSComponents = instance ?? throw new ArgumentNullException(nameof(instance));
        jsComponentTypesByIdentifier = (Dictionary<string, Type>) JSComponentTypesByIdentifierField.GetValue(JSComponents);
    }

    public IDictionary<string, Type> JsComponentTypesByIdentifier => jsComponentTypesByIdentifier;
    
    public JSComponentConfigurationStore JSComponents { get; }
}