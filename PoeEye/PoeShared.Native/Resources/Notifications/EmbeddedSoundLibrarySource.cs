using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using PoeShared.Audio.Services;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Resources.Notifications;

internal class EmbeddedSoundLibrarySource : SoundLibrarySourceBase, IEmbeddedSoundLibrarySource
{
    private static readonly IFluentLog Log = typeof(EmbeddedSoundLibrarySource).PrepareLogger();

    private static readonly string[] EmbeddedResourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
    private readonly ConcurrentDictionary<string, byte[]> sourceDataByName = new(StringComparer.OrdinalIgnoreCase);

    public EmbeddedSoundLibrarySource()
    {
        Log.Debug($"Embedded resources list:\r\n {EmbeddedResourceNames.DumpToString()}");

        var namespaceName = typeof(EmbeddedSoundLibrarySource).Namespace;
        var extensions = GetSupportedExtensions();
        var sources = EmbeddedResourceNames
            .Select(x => new {InternalResourceName = x, SourceName = Path.GetFileNameWithoutExtension(x.Replace($"{namespaceName}.", string.Empty))})
            .Where(x => extensions.Any(ext => string.Equals(ext, Path.GetExtension(x.InternalResourceName), StringComparison.OrdinalIgnoreCase)))
            .Select(x => x.SourceName)
            .ToArray();
        SourceName = new ReadOnlyObservableCollection<string>(new ObservableCollection<string>(sources));

        Log.Debug($"Source name list(namespace: {namespaceName}):\r\n {sources.DumpToString()}");
    }

    public override ReadOnlyObservableCollection<string> SourceName { get; }
        
    public override  bool TryToLoadSourceByName(string name, out byte[] waveData)
    {
        Log.Debug($"Resolving resource {name} (cache: {sourceDataByName.Count})");
        if (sourceDataByName.TryGetValue(name, out waveData))
        {
            Log.Debug($"Using cached source {name}");
            return true;
        }

        Log.Debug($"Trying to load resource {name}...");
        lock (sourceDataByName)
        {
            var success = TryToLoadSourceByNameInternal(name, out var newWaveData) && newWaveData != null;

            if (!success)
            {
                Log.Warn($"Failed to load resource {name}");
                waveData = default;
                return false;
            }
                
            Log.Debug($"Successfully loaded resource {name}");
            sourceDataByName[name] = newWaveData;
            waveData = newWaveData;
            return true;
        }
    }
        

    private bool TryToLoadSourceByNameInternal(string name, out byte[] resourceData)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var namespaceName = typeof(EmbeddedSoundLibrarySource).Namespace;
        var resourceNameCandidates = FormatFileName(name)
            .Select(x => $"{namespaceName}.{x}")
            .ToArray();
        Log.Debug($"Trying to find resource using names '{resourceNameCandidates.DumpToString()}'...");

        string resourceName = null;
        foreach (var embeddedResourceName in EmbeddedResourceNames)
        {
            foreach (var resourceNameCandidate in resourceNameCandidates)
            {
                if (!string.Equals(resourceNameCandidate, embeddedResourceName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (embeddedResourceName != resourceNameCandidate)
                {
                    Log.Debug($"Embedded resource name: '{embeddedResourceName}', candidate: {resourceNameCandidate}...");
                }
                resourceName = embeddedResourceName;
                break;
            }
        }

        if (string.IsNullOrEmpty(resourceName))
        {
            Log.Debug($"Failed to find internal resource name for '{name}'");

            resourceData = null;
            return false;
        }

        Log.Debug($"Loading resource '{resourceName}'...");
        var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream == null)
        {
            var resourcesList = assembly.GetManifestResourceNames();
            Log.Debug($"Resource was not found '{resourceName}', embedded res.list: {resourcesList.DumpToString()}");
            resourceData = null;
            return false;
        }

        using (var stream = resourceStream)
        {
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);

            Log.Debug($"Loaded resource '{resourceName}' : {buffer.Length}b");
            resourceData = buffer;
            return true;
        }
    }
}