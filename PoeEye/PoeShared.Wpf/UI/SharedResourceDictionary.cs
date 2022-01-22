using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Markup;
using log4net;
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

    /// <summary>
    /// Internal cache of loaded dictionaries 
    /// </summary>
    public static Dictionary<Uri, ResourceDictionary> SharedDictionaries { get; } = new();

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
                using var sw = new BenchmarkTimer($"Loading {nameof(SharedResourceDictionary)} from {value}", Log);
                try
                {
                    // If the dictionary is not yet loaded, load it by setting
                    // the source of the base class
                    base.Source = value;
                    sw.Step($"Loaded resources from {value}");
                }
                catch (Exception e)
                {
                    throw new ApplicationException($"Failed to load resource from {value}", e);
                }
                   
                // add it to the cache
                SharedDictionaries.Add(value, this);
                sw.Step($"Added resource {value} to cache, size: {SharedDictionaries.Count}");
            }
            else
            {
                // If the dictionary is already loaded, get it from the cache
                MergedDictionaries.Add(SharedDictionaries[value]);
            }
        }
    }
}