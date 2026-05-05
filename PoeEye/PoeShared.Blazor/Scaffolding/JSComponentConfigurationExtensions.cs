using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components.Web;

namespace PoeShared.Blazor.Scaffolding;

public static class JSComponentConfigurationExtensions
{
    private static readonly FieldInfo JSComponentTypesByIdentifierField =
        typeof(JSComponentConfigurationStore)
            .GetField("_jsComponentTypesByIdentifier", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new ApplicationException(
            $"Failed to get field _jsComponentTypesByIdentifier on type {typeof(JSComponentConfigurationStore)}");

    public static IReadOnlyDictionary<string, Type> GetRegisteredJavaScriptComponents(this IJSComponentConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return GetRegisteredJavaScriptComponents(configuration.JSComponents);
    }

    public static IReadOnlyDictionary<string, Type> GetRegisteredJavaScriptComponents(this JSComponentConfigurationStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        return GetRegisteredJavaScriptComponentsUnsafe(store);
    }

    public static bool RegisterForJavaScriptIfMissing(
        this IJSComponentConfiguration configuration,
        Type componentType,
        string identifier)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(componentType);
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identifier));
        }

        var registeredComponents = GetRegisteredJavaScriptComponentsUnsafe(configuration.JSComponents);
        if (registeredComponents.TryGetValue(identifier, out var existingComponentType))
        {
            if (existingComponentType == componentType)
            {
                return false;
            }

            throw new InvalidOperationException(
                $"JS component identifier '{identifier}' is already registered for type '{existingComponentType.FullName}', " +
                $"cannot register type '{componentType.FullName}'.");
        }

        configuration.RegisterForJavaScript(componentType, identifier);
        return true;
    }

    private static Dictionary<string, Type> GetRegisteredJavaScriptComponentsUnsafe(JSComponentConfigurationStore store)
    {
        var registeredComponents = JSComponentTypesByIdentifierField.GetValue(store) as Dictionary<string, Type>;
        if (registeredComponents == null)
        {
            throw new InvalidOperationException(
                $"Failed to get registered JS component map from {typeof(JSComponentConfigurationStore)}");
        }

        return registeredComponents;
    }
}
