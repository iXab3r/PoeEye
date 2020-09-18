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

        public static T GetPropertyValue<T>(this object model, string propertyName)
        {
            var propertyInfo = GetProperty(model, propertyName);
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
        
        public static object SetPropertyValue<T>(this object model, string propertyName, T value)
        {
            Guard.ArgumentNotNull(model, nameof(model));
            var property = GetProperty(model, propertyName);
            property.propertyInfo.SetValue(property.owner, value);
            return model;
        }
        
        private static (object owner, PropertyInfo propertyInfo) GetProperty(object model, string propertyName)
        {
            Guard.ArgumentNotNull(model, nameof(model));
            Guard.ArgumentNotNull(propertyName, nameof(propertyName));

            var propertyParts = propertyName.Split('.');
            if (propertyParts.Length <= 1)
            {
                return (model, PropertyAccessorByName.GetOrAdd(
                    (model.GetType(), propertyName),
                    x =>
                    {
                        var property = x.type.GetProperties().FirstOrDefault(y => string.Compare(x.propertyName, y.Name, StringComparison.OrdinalIgnoreCase) == 0);
                        if (property == null)
                        {
                            throw new ArgumentException($"Failed to find property {propertyName} in model {model} of type {model.GetType()}");
                        }

                        return property;
                    }));
            }

            var rootProperty = GetProperty(model, propertyParts[0]);
            var root = rootProperty.propertyInfo.GetValue(model);
            return GetProperty(root, propertyParts.Skip(1).JoinStrings("."));
        }
    }
}