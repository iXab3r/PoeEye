using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using PoeShared.Scaffolding;
using PoeShared.Logging;

namespace PoeShared.UI;

/// <summary>
/// The shared resource dictionary is a specialized resource dictionary
/// that loads it content only once. If a second instance with the same source
/// is created, it only merges the resources from the cache.
/// http://www.wpftutorial.net/MergedDictionaryPerformance.html
/// </summary>
public class SharedResourceDictionary : ResourceDictionary
{
    private static readonly IFluentLog Log = typeof(SharedResourceDictionary).PrepareLogger();

    [ThreadStatic] private static Dictionary<Uri, ResourceDictionary> sharedDictionaries;

    /// <summary>
    /// Internal cache of loaded dictionaries 
    /// </summary>
    public static Dictionary<Uri, ResourceDictionary> SharedDictionaries => sharedDictionaries ??= new Dictionary<Uri, ResourceDictionary>();


    /// <summary>
    /// Local member of the source uri
    /// </summary>
    private Uri sourceUri;

    /// <summary>
    /// Gets or sets the uniform resource identifier (URI) to load resources from.
    /// </summary>
    public new Uri Source
    {
        get => sourceUri;
        set
        {
            sourceUri = value;
            if (!SharedDictionaries.ContainsKey(value))
            {
                using var sw = new BenchmarkTimer(Log);
                sw.Step(() => $"Loading {nameof(SharedResourceDictionary)} from {value}");
                try
                {
                    // If the dictionary is not yet loaded, load it by setting
                    // the source of the base class
                    base.Source = value;
                    sw.Step(() => $"Loaded resources from {value}");
                }
                catch (Exception e)
                {
                    throw new ApplicationException($"Failed to load resource from {value}", e);
                }

                // add it to the cache
                SharedDictionaries.Add(value, this);
                sw.Step(() => $"Added resource {value} to cache, size: {SharedDictionaries.Count}");
            }
            else
            {
                // If the dictionary is already loaded, get it from the cache
                MergedDictionaries.Add(SharedDictionaries[value]);
            }
        }
    }

#if DEBUG
    [ThreadStatic] private static Stack<object> resourceResolutionQueue;
    private Stack<object> ResourceResolutionQueue => resourceResolutionQueue ??= new();

    protected override void OnGettingValue(object key, ref object value, out bool canCache)
    {
        string GetKey()
        {
            return resourceResolutionQueue.DumpToTable(" -> ");
        }

        ResourceResolutionQueue.Push(key);
        var mappedKey = GetKey();

        try
        {
            using var sw = new BenchmarkTimer(Log.WithSuffix(mappedKey)).WithoutLoggingEachStep();
            sw.Step(() => $"Resolving resource");
            base.OnGettingValue(key, ref value, out canCache);
            if (value is not DispatcherObject dispatcherObject)
            {
                sw.Step($"Resolved non-dispatcher resource, canCache: {canCache}");
                return;
            }

            if (!dispatcherObject.CheckAccess())
            {
                var message = $"Resolved object by key {key} is owned by another dispatcher, current: {dispatcherObject.Dispatcher.Thread.Name}, expected: {Dispatcher.CurrentDispatcher.Thread.Name}";
                sw.Step(message);
                throw new InvalidOperationException(message);
            }

            sw.Step($"Resolved resource, canCache: {canCache}");
        }
        finally
        {
            var dequeue = ResourceResolutionQueue.Pop();
            if (dequeue != key)
            {
                throw new InvalidOperationException($"Expected {key} (full {mappedKey}), got {dequeue}, queue: {GetKey()}");
            }
        }
    }
#else
    protected override void OnGettingValue(object key, ref object value, out bool canCache)
    {
        base.OnGettingValue(key, ref value, out canCache);
        if (value is not DispatcherObject dispatcherObject)
        {
            return;
        }
        
        if (!dispatcherObject.CheckAccess())
        {
            var message = $"Resolved object by key {key} is owned by another dispatcher, current: {dispatcherObject.Dispatcher.Thread.Name}, expected: {Dispatcher.CurrentDispatcher.Thread.Name}";
            Log.Warn(message);
            throw new InvalidOperationException(message);
        }
    }
#endif
}