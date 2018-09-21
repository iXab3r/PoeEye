using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters;
using Common.Logging;
using Guards;
using Newtonsoft.Json;

namespace PoeShared.Scaffolding
{
    public static class ObjectExtensions
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ObjectExtensions));

        public static string DumpToText<T>(this T instance)
        {
            return DumpToText(instance, Formatting.Indented);
        }

        public static string DumpToTextRaw<T>(this T instance)
        {
            return DumpToText(instance, Formatting.None);
        }

        public static string DumpToTable<T>(this IEnumerable<T> instance, string separator = "\n\t")
        {
            return instance == null ? $"null<{typeof(T).Name}>" : string.Join(separator, instance.Select(x => x.DumpToTextRaw()));
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
            Guard.ArgumentNotNull(target, nameof(target));

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
                Log.Debug(
                    $"[TransferProperties] Skipped following properties: {skippedProperties.Select(x => $"{x.PropertyType} {x.Name}").DumpToTextRaw()}");
            }
        }

        public static void CopyPropertiesTo<TSource, TTarget>(this TSource source, TTarget target)
            where TTarget : class, TSource
        {
            var deserializeSettings = new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full
            };
            var serializedObject = JsonConvert.SerializeObject(source, deserializeSettings);
            JsonConvert.PopulateObject(serializedObject, target, deserializeSettings);
        }

        /// <summary>
        ///     Perform a deep Copy of the object, using Json as a serialisation method. NOTE: Private members are not cloned using this method.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>The copied object.</returns>
        public static T CloneJson<T>(this T source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (ReferenceEquals(source, null))
            {
                return default(T);
            }

            // initialize inner objects individually
            // for example in default constructor some list property initialized with some values,
            // but in 'source' these items are cleaned -
            // without ObjectCreationHandling.Replace default constructor values will be added to result
            var deserializeSettings = new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full
            };
            var json = JsonConvert.SerializeObject(source, deserializeSettings);
            return JsonConvert.DeserializeObject<T>(json, deserializeSettings);
        }
    }
}