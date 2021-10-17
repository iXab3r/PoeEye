using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace PoeShared.Scaffolding
{
    //FIXME Should be rewritten to expression trees
    public static class ObjectReflectionExtensions
    {
        private static readonly ConcurrentDictionary<(Type type, string propertyName), PropertyInfo> PropertyAccessorByName = new ConcurrentDictionary<(Type type, string propertyName), PropertyInfo>();

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

        public static PropertyInfo GetPropertyInfo(this Type type, string propertyPath)
        {
            var propertyParts = propertyPath.Split('.');
            if (propertyParts.Length > 1)
            {
                var rootProperty = GetPropertyInfo(type, propertyParts[0]);
                return GetPropertyInfo(rootProperty.PropertyType, propertyParts.Skip(1).JoinStrings("."));
            }
            
            return PropertyAccessorByName.GetOrAdd(
                (type, propertyPath),
                x =>
                {
                    var property = x.type.GetProperties().FirstOrDefault(y => string.Compare(x.propertyName, y.Name, StringComparison.OrdinalIgnoreCase) == 0);
                    if (property == null)
                    {
                        throw new ArgumentException($"Failed to find property {propertyPath} in type {type}");
                    }

                    return property;
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
}