using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Services;

public interface IStaticWebAssetsFileProvider : IFileProvider
{
}

internal sealed class StaticWebAssetsFileProvider : IStaticWebAssetsFileProvider
{
    private static readonly IFluentLog Log = typeof(StaticWebAssetsFileProvider).PrepareLogger();

    private readonly IFileProvider staticWebAssetsFileProvider;
    
    public StaticWebAssetsFileProvider()
    {
        staticWebAssetsFileProvider = UseStaticWebAssetsViaReflection(new InMemoryFileProvider());
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        return staticWebAssetsFileProvider.GetFileInfo(subpath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return staticWebAssetsFileProvider.GetDirectoryContents(subpath);
    }

    public IChangeToken Watch(string filter)
    {
        return staticWebAssetsFileProvider.Watch(filter);
    }

    private static IFileProvider UseStaticWebAssetsViaReflection(IFileProvider originalProvider)
    {
        var type = typeof(WebViewManager).Assembly.GetType("Microsoft.AspNetCore.Components.WebView.WebViewManager+StaticWebAssetsLoader", throwOnError: true);
        var method = type!.GetMethod("UseStaticWebAssets", BindingFlags.Static | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException("UseStaticWebAssets method not found.");
        }

        var result = method.Invoke(null, new object[] { originalProvider });
        if (result == null)
        {
            throw new InvalidOperationException("Failed to create static web assets file provider.");
        }

        if (result is not CompositeFileProvider compositeFileProvider)
        {
            Log.Info("Could not create StaticWebAssets provider - manifest may be missing, normal for released/published apps");
            return originalProvider;
        }

        Log.Info("Created StaticWebAssets provider");
        return compositeFileProvider;
    }
}