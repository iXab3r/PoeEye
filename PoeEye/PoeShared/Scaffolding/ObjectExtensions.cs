using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using CommandLine;
using DynamicData;
using Newtonsoft.Json;
using PoeShared.Modularity;
using ReactiveUI;

namespace PoeShared.Scaffolding;

public static class ObjectExtensions
{
    private static readonly IFluentLog Log = typeof(ObjectExtensions).PrepareLogger();

    private static readonly ConcurrentDictionary<Type, IReadOnlyCollection<PropertyInfo>> ReadablePropertiesMapByType = new ConcurrentDictionary<Type, IReadOnlyCollection<PropertyInfo>>();
    private static readonly ConcurrentDictionary<Type, IReadOnlyCollection<PropertyInfo>> WriteablePropertiesMapByType = new ConcurrentDictionary<Type, IReadOnlyCollection<PropertyInfo>>();

    public static string Dump<T>(this T instance)
    {
        return ToStringSafe(instance);
    }

    [Obsolete("Replaced with Dump(), left here for compatibility reasons")]
    public static string DumpToText<T>(this T instance)
    {
        return instance.Dump();
    }

    [Obsolete("Replaced with Dump(), left here for compatibility reasons")]
    public static string DumpToTextRaw<T>(this T instance)
    {
        return instance.Dump();
    }

    public static string DumpToString<T>(this IEnumerable<T> instance)
    {
        return DumpToTable(instance, ", ");
    }

    public static string DumpToTable<T>(this IEnumerable<T> instance, string separator = "\n\t")
    {
        return instance.DumpToTable(separator: separator, name: null, maxItemsToShow: null);
    }

    public static string DumpToNamedTable<T>(this IEnumerable<T> instance, string name, string separator = "\n\t")
    {
        return instance.DumpToTable(separator: separator, name: name, maxItemsToShow: null);
    }

    public static string DumpToNamedTable<T>(this IEnumerable<T> instance, string name, int maxItemsToShow, string separator = "\n\t")
    {
        return instance.DumpToTable(separator: separator, name: name, maxItemsToShow: maxItemsToShow);
    }

    public static string DumpToTable<T>(
        this IEnumerable<T> instance, 
        string name,
        string separator,
        int? maxItemsToShow)
    {
        if (instance == null)
        {
            return instance.Dump();
        }
        var result = new StringBuilder();
        var itemCount = 0;
        foreach (var item in instance)
        {
            if (!maxItemsToShow.HasValue || itemCount <= maxItemsToShow)
            {
                result.Append(separator);
                result.Append(item.Dump());
            }
            itemCount++;
        }

        if (itemCount > maxItemsToShow)
        {
            result.Append(separator);
            result.Append($"and {itemCount - maxItemsToShow} more...");
        }

        if (!string.IsNullOrEmpty(name))
        {
            result.Insert(0, $"{name}({itemCount}):");
        }
        return result.ToString();
    }
    
    public static string DumpToHex(this byte[] value, int bytesPerLine = 32)
    {
        if (value == null)
        {
            return "null<byte[]>";
        }
        return StringUtils.HexDump(value, bytesPerLine);
    }

    [Obsolete("Replaced with DumpToTable(), left here for compatibility reasons")]
    public static string ToStringTable<T>(this IEnumerable<T> instance, string separator = "\n\t")
    {
        return instance.DumpToTable(separator);
    }

    public static string ToStringSafe<T>(this T instance)
    {
        return ToHumanReadable(instance);
    }

    private static string ToHumanReadable<T>(this T item)
    {
        if (item is IntPtr ptr)
        {
            return ptr.ToHexadecimal();
        }
        return item == null ? "null" : item.ToString();
    }

    public static TItem AddTo<TItem, TCollection>(this TItem instance, ISourceList<TCollection> parent) where TItem : TCollection
    {
        Guard.ArgumentNotNull(instance, nameof(instance));
        Guard.ArgumentNotNull(parent, nameof(parent));

        parent.Add(instance);
        return instance;
    }

    public static TItem AddTo<TItem, TCollection>(this TItem instance, ICollection<TCollection> collection) where TItem : TCollection
    {
        Guard.ArgumentNotNull(instance, nameof(instance));
        Guard.ArgumentNotNull(collection, nameof(collection));

        collection.Add(instance);
        return instance;
    }

    public static TItem InsertTo<TItem, TCollection>(this TItem instance, IList<TCollection> collection, int index) where TItem : TCollection
    {
        Guard.ArgumentNotNull(instance, nameof(instance));
        Guard.ArgumentNotNull(collection, nameof(collection));

        collection.Insert(index, instance);
        return instance;
    }

    public static IEnumerable<PropertyInfo> GetAllProperties(this Type type, BindingFlags flags)
    {
        if (!type.IsInterface)
        {
            return type.GetProperties(flags);
        }

        return new[] { type }
            .Concat(type.GetInterfaces())
            .SelectMany(i => i.GetProperties(flags));
    }

    public static void TransferPropertiesTo<TSource, TTarget>(this TSource source, TTarget target)
        where TTarget : class, TSource
    {
        Guard.ArgumentNotNull(source, nameof(source));
        Guard.ArgumentNotNull(target, nameof(target));

        var targetProperties = WriteablePropertiesMapByType
            .GetOrAdd(typeof(TTarget), type => type
                .GetAllProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead && x.CanWrite)
                .ToArray());
            
        var sourceProperties = ReadablePropertiesMapByType
            .GetOrAdd(typeof(TSource), type => type
                .GetAllProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead)
                .ToArray());

        var skippedProperties = new List<PropertyInfo>();
        foreach (var property in sourceProperties)
        {
            try
            {
                var settableProperty = targetProperties
                    .FirstOrDefault(x => x.Name == property.Name && x.PropertyType.IsAssignableFrom(property.PropertyType));
                if (settableProperty == null)
                {
                    skippedProperties.Add(property);
                    continue;
                }

                var currentValue = property.GetValue(source);
                settableProperty.SetValue(target, currentValue);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"Exception occurred, property: {property}\r\n" +
                    $"Target properties: {targetProperties.Select(x => $"{x.PropertyType} {x.Name}").DumpToString()}\r\n" +
                    $"Source properties: {sourceProperties.Select(x => $"{x.PropertyType} {x.Name}").DumpToString()}",
                    ex);
            }
        }

        if (skippedProperties.Any())
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug(() => $"Skipped following properties: {skippedProperties.Select(x => $"{x.PropertyType} {x.Name}").DumpToString()}");
            }
        }
    }

    public static void CopyPropertiesTo<TSource, TTarget>(this TSource source, TTarget target)
        where TTarget : class, TSource
    {
        var deserializeSettings = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
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
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
        };
        var json = JsonConvert.SerializeObject(source, deserializeSettings);
        return JsonConvert.DeserializeObject<T>(json, deserializeSettings);
    }

    public static TValue EvalOrDefault<T, TValue>(this T instance, Func<T, TValue> extractor, TValue defaultValue) where T : class
    {
        if (instance == default)
        {
            return defaultValue;
        }

        return extractor(instance);
    }

    public static TValue Eval<T, TValue>(this T instance, Func<T, TValue> extractor) where T : class
    {
        return EvalOrDefault(instance, extractor, default);
    }

    public static PropertyAccessor<TValue> GetPropertyAccessor<TValue>(
        this object source, 
        string propertyPath)
    {
        return new PropertyAccessor<TValue>(source, propertyPath);
    }
    
    public static PropertyAccessor<TValue> GetPropertyAccessor<TSource, TValue>(
        this TSource source, 
        Expression<Func<TSource, TValue>> valueAccessor)
    {
        var propertyPath = Reflection.ExpressionToPropertyNames(valueAccessor.Body);
        return new PropertyAccessor<TValue>(source, propertyPath);
    }
}