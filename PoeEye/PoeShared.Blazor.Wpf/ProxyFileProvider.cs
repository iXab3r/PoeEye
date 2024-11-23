using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace PoeShared.Blazor.Wpf;

internal sealed class ProxyFileProvider : IFileProvider
{
    public IFileProvider FileProvider { get; set; }
    
    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        var provider = FileProvider;
        return provider == null ? NotFoundDirectoryContents.Singleton : provider.GetDirectoryContents(subpath);
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        var provider = FileProvider;
        return provider == null ?  new NotFoundFileInfo(subpath) : provider.GetFileInfo(subpath);
    }

    public IChangeToken Watch(string filter)
    {
        var provider = FileProvider;
        return provider == null ? NullChangeToken.Singleton : provider.Watch(filter);
    }
}