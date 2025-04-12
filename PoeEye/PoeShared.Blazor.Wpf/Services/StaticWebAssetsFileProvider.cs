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
    FileInfo RuntimeAssetsFile { get; }
}

internal sealed class StaticWebAssetsFileProvider : IStaticWebAssetsFileProvider
{
    private static readonly IFluentLog Log = typeof(StaticWebAssetsFileProvider).PrepareLogger();

    private readonly IFileProvider staticWebAssetsFileProvider;

    public StaticWebAssetsFileProvider(FileInfo staticWebFileRuntimeJsonFile)
    {
        Log.Info($"Initializing static web assets file provider @ {staticWebFileRuntimeJsonFile.FullName} (exists: {staticWebFileRuntimeJsonFile.Exists})");
        RuntimeAssetsFile = staticWebFileRuntimeJsonFile;
        staticWebAssetsFileProvider = CreateStaticWebAssetsFileProvider(new InMemoryFileProvider(), staticWebFileRuntimeJsonFile);
    }

    public StaticWebAssetsFileProvider() : this(ResolveRelativeToAssembly())
    {
    }

    public FileInfo RuntimeAssetsFile { get; }

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

    private static IFileProvider CreateStaticWebAssetsViaReflection(IFileProvider originalProvider)
    {
        var type = typeof(WebViewManager).Assembly.GetType("Microsoft.AspNetCore.Components.WebView.WebViewManager+StaticWebAssetsLoader", throwOnError: true);
        var method = type!.GetMethod("UseStaticWebAssets", BindingFlags.Static | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException("UseStaticWebAssets method not found.");
        }

        var result = method.Invoke(null, new object[] {originalProvider});
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

    private static IFileProvider CreateStaticWebAssetsFileProvider(IFileProvider fileProvider, FileInfo staticWebFileRuntimeJsonFile)
    {
        var webViewAssembly = typeof(WebViewManager).Assembly;

        var manifestFileProviderType = webViewAssembly.GetType("Microsoft.AspNetCore.StaticWebAssets.ManifestStaticWebAssetFileProvider", throwOnError: true);

        var manifestFileProviderCtors = manifestFileProviderType!.GetConstructors();
        var manifestFileProviderCtor = manifestFileProviderCtors.FirstOrDefault(x => x.GetParameters().Length == 2);
        if (manifestFileProviderCtor == null)
        {
            throw new InvalidOperationException("ManifestStaticWebAssetFileProvider does not have expected constructor.");
        }

        var manifestType = webViewAssembly.GetType(manifestFileProviderType!.FullName + "+StaticWebAssetManifest");
        var parseMethod = manifestType!.GetMethod("Parse", BindingFlags.Static | BindingFlags.NonPublic);
        if (parseMethod == null)
        {
            throw new InvalidOperationException("StaticWebAssets Manifest Parse method not found.");
        }

        var contentRootsProperty = manifestType!.GetProperty("ContentRoots", BindingFlags.Instance | BindingFlags.Public);
        if (contentRootsProperty == null)
        {
            throw new InvalidOperationException("StaticWebAssets Manifest ContentRoots property not found.");
        }

        if (!staticWebFileRuntimeJsonFile.Exists)
        {
            return fileProvider;
        }

        using var manifestStream = File.OpenRead(staticWebFileRuntimeJsonFile.FullName);
        var manifest = parseMethod.Invoke(null, new object[] {manifestStream});
        if (manifest == null)
        {
            throw new InvalidOperationException("Could not parse manifest.");
        }

        var contentRoots = (string[]) contentRootsProperty.GetValue(manifest);
        if (contentRoots == null)
        {
            throw new InvalidOperationException("Content roots property not set");
        }

        if (contentRoots.Length <= 0)
        {
            return fileProvider;
        }

        var pathFunc = new Func<string, IFileProvider>((path) => new PhysicalFileProvider(path));
        var manifestProvider = (IFileProvider) Activator.CreateInstance(manifestFileProviderType, new[] {manifest, pathFunc}); // (IFileProvider) manifestFileProviderCtor.Invoke(null, new[] {manifest, pathFunc});
        if (manifestProvider == null)
        {
            throw new InvalidOperationException("Could not create manifest file provider");
        }

        return manifestProvider;
    }

    private static FileInfo ResolveRelativeToAssembly()
    {
        var assembly = Assembly.GetEntryAssembly();
        if (string.IsNullOrEmpty(assembly?.Location))
        {
            return null;
        }

        var name = Path.GetFileNameWithoutExtension(assembly.Location);

        var path = Path.Combine(Path.GetDirectoryName(assembly.Location)!, $"{name}.staticwebassets.runtime.json");
        return new FileInfo(path); 
    }

}