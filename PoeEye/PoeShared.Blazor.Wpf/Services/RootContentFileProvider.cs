using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Services;

/// <summary>
/// A composite file provider that searches for a valid 'wwwroot' directory in known locations,
/// such as the entry assembly, executing assembly, or current working directory.
/// If found, it registers a physical file provider for it; otherwise, it falls back to an empty provider.
/// </summary>
internal sealed class RootContentFileProvider : IRootContentFileProvider
{
    private static readonly IFluentLog Log = typeof(RootContentFileProvider).PrepareLogger();

    private readonly IFileProvider fileProvider;

    public RootContentFileProvider()
    {
        var pathsToProbe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null && !string.IsNullOrEmpty(entryAssembly.Location))
        {
            pathsToProbe.Add(Path.GetDirectoryName(entryAssembly.Location));
        }

        var executingAssembly = Assembly.GetExecutingAssembly();
        if (!string.IsNullOrEmpty(executingAssembly.Location))
        {
            pathsToProbe.Add(Path.GetDirectoryName(executingAssembly.Location));
        }

        var currentDirectory = Environment.CurrentDirectory;
        pathsToProbe.Add(currentDirectory);

        var providersToRegister = new List<IFileProvider>();
        foreach (var path in pathsToProbe)
        {
            if (IsValidWwwRootDirectory(path, out var provider))
            {
                providersToRegister.Add(provider);
            }
        }

        Log.Info($"📦 Registering {providersToRegister.Count} content file providers");
        fileProvider = providersToRegister.Count switch
        {
            <= 0 => new InMemoryFileProvider(),
            1 => providersToRegister[0],
            _ => new CompositeFileProvider(providersToRegister)
        };
    }

    private static bool IsValidWwwRootDirectory(string directoryPath, out IFileProvider fileProvider)
    {
        if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
        {
            fileProvider = null;
            return false;
        }

        var wwwrootPath = new DirectoryInfo(Path.Combine(directoryPath, "wwwroot"));
        Log.Info($"🌐 Checking wwwroot folder: {wwwrootPath} (Exists: {wwwrootPath.Exists})");

        var staticWebAssetsManifestPath = Path.Combine(directoryPath, $"{Path.GetFileNameWithoutExtension(directoryPath)}.staticwebassets.runtime.json");
        var hasStaticWebAssets = File.Exists(staticWebAssetsManifestPath);
        Log.Info($"📦 Looking for staticwebassets manifest: {staticWebAssetsManifestPath} (Exists: {hasStaticWebAssets})");

        if (!wwwrootPath.Exists)
        {
            fileProvider = null;
            return false;
        }

        Log.Info($"✅ Registering caching file provider from directory: {wwwrootPath.FullName}");
        fileProvider = CachingPhysicalFileProvider.GetOrAdd(wwwrootPath);
        return true;
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        return fileProvider.GetFileInfo(subpath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return fileProvider.GetDirectoryContents(subpath);
    }

    public IChangeToken Watch(string filter)
    {
        return fileProvider.Watch(filter);
    }
}