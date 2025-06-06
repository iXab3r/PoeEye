﻿using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PoeShared.Scaffolding;

//FIXME Should be rewritten to expression trees
public static class ObjectReflectionExtensions
{
    private static readonly ConcurrentDictionary<(Type type, string propertyName), PropertyInfo> PropertyAccessorByName = new();

    private static readonly Regex PropertyPathRegexValidator = new Regex(@"^[\w\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly string ExternalInitTypeFullName = typeof(System.Runtime.CompilerServices.IsExternalInit).FullName;
    
    public static T GetPropertyValue<T>(this object model, string propertyPath)
    {
        var propertyInfo = GetProperty(model, propertyPath);
        var result = propertyInfo.propertyInfo.GetValue(propertyInfo.owner);
        try
        {
            if (result is T)
            {
                return (T) result;
            }

            return (T) Convert.ChangeType(result, typeof(T));
        }
        catch (Exception e)
        {
            throw new InvalidCastException($"Failed to cast value {result} of type {(result?.GetType().Name ?? "Null")} to {typeof(T)}", e);
        }
    }

    public static Type GetPropertyTypeOrDefault(this object instance, string propertyPath)
    {
        if (instance == null || string.IsNullOrEmpty(propertyPath))
        {
            return default;
        }

        var type = instance.GetType();
        if (!IsValidPropertyPath(propertyPath))
        {
            throw new ArgumentException($"Invalid property format: {propertyPath}, type: {type}");
        }

        return type.GetPropertyTypeOrDefault(propertyPath);
    }

    private static bool IsValidPropertyPath(string propertyPath)
    {
        return PropertyPathRegexValidator.IsMatch(propertyPath);
    }

    public static Type GetPropertyTypeOrDefault(this Type type, string propertyPath)
    {
        return GetPropertyInfoOrDefault(type, propertyPath)?.PropertyType;
    }

    public static PropertyInfo GetPropertyInfoOrDefault(this Type type, string propertyPath)
    {
        if (type == null || string.IsNullOrEmpty(propertyPath))
        {
            return default;
        }
        if (!IsValidPropertyPath(propertyPath))
        {
            throw new ArgumentException($"Invalid property format: {propertyPath}, type: {type}");
        }
        var propertyParts = propertyPath.Split('.');
        if (propertyParts.Length > 1)
        {
            var rootProperty = GetPropertyInfoOrDefault(type, propertyParts[0]);
            return GetPropertyInfoOrDefault(rootProperty?.PropertyType, propertyParts.Skip(1).JoinStrings("."));
        }
            
        return PropertyAccessorByName.GetOrAdd(
            (type, propertyPath),
            x =>
            {
                var property = x.type.GetAllProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(y => string.Compare(x.propertyName, y.Name, StringComparison.OrdinalIgnoreCase) == 0);
                return property;
            });;
    }
        
    public static PropertyInfo GetPropertyInfo(this Type type, string propertyPath)
    {
        if (!IsValidPropertyPath(propertyPath))
        {
            throw new ArgumentException($"Invalid property format: {propertyPath}, type: {type}");
        }

        var propertyInfo = GetPropertyInfoOrDefault(type, propertyPath);
        if (propertyInfo == null)
        {
            throw new ArgumentException($"Failed to find property {propertyPath} in type {type}");
        }

        return propertyInfo;
    }

    /// <summary>
    /// Determines whether the specified <see cref="PropertyInfo"/> represents an indexed property.
    /// </summary>
    /// <param name="propertyInfo">The property to inspect.</param>
    /// <returns><c>true</c> if the property is an indexed property; otherwise, <c>false</c>.</returns>
    public static bool IsIndexedProperty(this PropertyInfo propertyInfo)
    {
        return propertyInfo.GetIndexParameters().Length > 0;
    }
    
    /// <summary>
    /// Determines whether the specified <see cref="PropertyInfo"/> represents an init-only property.
    /// </summary>
    /// <param name="propertyInfo">The property to inspect.</param>
    /// <returns><c>true</c> if the property is marked as init-only; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// Init-only properties are marked with the <c>System.Runtime.CompilerServices.IsExternalInit</c> modifier
    /// in their metadata, which is a feature introduced in C# 9 for immutable objects.
    /// </remarks>
    public static bool IsInitOnly(this PropertyInfo propertyInfo)
    {
        var setMethod = propertyInfo.SetMethod;
        if (setMethod == null)
        {
            return false; // No setter = cannot be init-only-
        }

        var returnParameter = setMethod.ReturnParameter;
        if (returnParameter == null)
        {
            return false;
        }

        var modreq = returnParameter.GetRequiredCustomModifiers();
        if (modreq.Length == 0)
        {
            return false;
        }
        
        return modreq.Any(type =>
        {
            //comparing via Name as EA scripting system could have multiple different ExternalInit types loaded in
            return string.Equals(type.FullName, ExternalInitTypeFullName, StringComparison.Ordinal);
        });
    }

    public static object SetPropertyValue<T>(this object model, string propertyPath, T value)
    {
        Guard.ArgumentNotNull(model, nameof(model));
        var property = GetProperty(model, propertyPath);
        property.propertyInfo.SetValue(property.owner, value);
        return model;
    }

    private static (object owner, PropertyInfo propertyInfo) GetProperty(object model, string propertyPath)
    {
        Guard.ArgumentNotNull(model, nameof(model));
        Guard.ArgumentNotNull(propertyPath, nameof(propertyPath));

        var propertyParts = propertyPath.Split('.');
        if (propertyParts.Length <= 1)
        {
            return (model, GetPropertyInfo(model.GetType(), propertyPath));
        } 
            
        var rootProperty = GetProperty(model, propertyParts[0]);
        var root = rootProperty.propertyInfo.GetValue(model);
        return GetProperty(root, propertyParts.Skip(1).JoinStrings("."));
    }
    
}