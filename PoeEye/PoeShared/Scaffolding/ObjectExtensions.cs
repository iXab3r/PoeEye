﻿namespace PoeShared.Scaffolding
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Guards;

    using Newtonsoft.Json;

    public static class ObjectExtensions
    {
        public static string DumpToText<T>(this T instance)
        {
            return DumpToText(instance, Formatting.Indented);   
        }

        public static string DumpToTextRaw<T>(this T instance)
        {
            return DumpToText(instance, Formatting.None);
        }

        public static string DumpToText<T>(this T instance, Formatting formatting)
        {
            return instance == null ? $"null<{typeof(T).Name}>" : JsonConvert.SerializeObject(instance, formatting);
        }
        
        public static TItem AddTo<TItem, TCollection>(this TItem instance, ICollection<TCollection> collection) where TItem : TCollection
        {
            Guard.ArgumentNotNull(instance, nameof(instance));
            Guard.ArgumentNotNull(collection, nameof(collection));
            
            collection.Add(instance);
            return instance;
        }

        public static void TransferPropertiesTo<TSource, TTarget>(this TSource source, TTarget target)
            where TTarget : class, TSource
        {
            Guard.ArgumentNotNull(source, nameof(source));

            var settableProperties = typeof(TTarget)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead && x.CanWrite)
                .ToArray();

            var propertiesToSet = typeof(TSource)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead)
                .ToArray();

            var skippedProperties = new List<PropertyInfo>();
            foreach (var property in propertiesToSet)
            {
                try
                {
                    var currentValue = property.GetValue(source);

                    var settableProperty = settableProperties.FirstOrDefault(x => x.Name == property.Name);
                    if (settableProperty == null)
                    {
                        skippedProperties.Add(property);
                        continue;
                    }
                    settableProperty.SetValue(target, currentValue);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(
                        $"Exception occurred, property: {property}\r\n" +
                        $"Settable properties: {settableProperties.Select(x => $"{x.PropertyType} {x.Name}").DumpToText()}\r\n" +
                        $"PropertiesToSet: {propertiesToSet.Select(x => $"{x.PropertyType} {x.Name}").DumpToText()}",
                        ex);
                }
            }
            if (skippedProperties.Any())
            {
                Log.Instance.Debug($"[TransferProperties] Skipped following properties:\r\n{skippedProperties.Select(x => $"{x.PropertyType} {x.Name}").DumpToText()}");
            }
        }
    }
}
