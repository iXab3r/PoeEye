using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Services;

/// <summary>
/// Wraps the shared <see cref="IRootContentFileProvider"/> with script-local file providers so a script can expose
/// additional Blazor content roots without mutating the global application provider.
/// </summary>
/// <remarks>
/// This solves the isolation problem for script-hosted <c>BlazorWindow</c> instances: lookups first probe
/// script-specific providers such as NuGet <c>staticwebassets</c> or dynamically added content roots, then fall back
/// to the shared application-level provider. As a result, one script can resolve its own <c>_content/...</c> assets
/// without leaking those roots into other scripts.
/// </remarks>
public sealed class ScriptRootContentFileProvider : IRootContentFileProvider
{
    private static readonly IFluentLog Log = typeof(ScriptRootContentFileProvider).PrepareLogger();

    private readonly IRootContentFileProvider fallbackProvider;
    private readonly ReactiveCompositeFileProvider localProviders;
    private readonly ReactiveCompositeFileProvider fileProviders;

    public ScriptRootContentFileProvider(IRootContentFileProvider fallbackProvider, params IFileProvider[] additionalProviders)
    {
        this.fallbackProvider = fallbackProvider;
        localProviders = new ReactiveCompositeFileProvider(additionalProviders.Where(x => x != null));
        fileProviders = new ReactiveCompositeFileProvider(localProviders, fallbackProvider);
    }

    public void AddRuntimeAssetsFile(Assembly assembly)
    {
        localProviders.Add(new StaticWebAssetsFileProvider(assembly));
    }

    public void AddRuntimeAssetsFile(FileInfo fileInfo)
    {
        localProviders.Add(new StaticWebAssetsFileProvider(fileInfo));
    }

    public void AddContentRoot(DirectoryInfo contentRoot)
    {
        Log.Info($"Trying to add script-local content root directory: {contentRoot.FullName} (exists: {contentRoot.Exists})");
        if (!contentRoot.Exists)
        {
            throw new InvalidOperationException($"Could not add content root directory: {contentRoot.FullName}");
        }

        localProviders.Add(CachingPhysicalFileProvider.GetOrAdd(contentRoot));
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        return fileProviders.GetFileInfo(subpath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return fileProviders.GetDirectoryContents(subpath);
    }

    public IChangeToken Watch(string filter)
    {
        return fileProviders.Watch(filter);
    }
}
