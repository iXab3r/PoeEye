using System;
using System.IO;
using System.Linq;
using ByteSizeLib;
using DynamicData;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using IFileInfo = Microsoft.Extensions.FileProviders.IFileInfo;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// This file provider caches pre-loads all files inside content root which measurably reduces access time on slow systems.
/// Also this fixes a problem with virtualized file system - file watchers which are used by PhysicalFileProvider are not stable
/// </summary>
public sealed class CachingFileProvider : IFileProvider
{
    private static readonly IFluentLog Log = typeof(CachingFileProvider).PrepareLogger();

    private readonly InMemoryFileProvider memoryCache = new();

    public CachingFileProvider(DirectoryInfo contentRoot)
    {
        ContentRoot = contentRoot;
        Log.AddSuffix($"Root: {contentRoot.FullName}");
        Log.Debug("Initializing new caching file provider");
        if (contentRoot.Exists)
        {
            var directoryFiles = contentRoot.GetFiles("*", SearchOption.AllDirectories).Select(x => new
            {
                RelativePath=Path.GetRelativePath(contentRoot.FullName, x.FullName),
                x.LastWriteTime, 
                Size = ByteSize.FromBytes(x.Length),
                x.FullName,
            }).ToArray();
            Log.Debug($"Directory exists, files:\n\t{directoryFiles.DumpToTable()}");
            foreach (var file in directoryFiles)
            {
                Log.Debug($"Pre-loading file into cache: {file}");
                try
                {
                    memoryCache.FilesByName.AddOrUpdate(new InMemoryFileInfo(file.RelativePath, File.ReadAllBytes(file.FullName), file.LastWriteTime));
                }
                catch (Exception e)
                {
                    Log.Warn($"Failed to load into memory content of file {file.FullName}", e);
                }
            }
        }
        else
        {
            Log.Warn("Root directory does not exist");
        }
    }
    
    public DirectoryInfo ContentRoot { get; }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        var result = memoryCache.GetDirectoryContents(subpath);
        if (result == null)
        {
            Log.Warn($"Failed to get contents of directory {subpath}");
        }
        return result;
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        var result = memoryCache.GetFileInfo(subpath);
        if (result == null)
        {
            Log.Warn($"Failed to content of file {subpath}");
        }
        return result;
    }

    public IChangeToken Watch(string filter)
    {
        return memoryCache.Watch(filter);
    }
}