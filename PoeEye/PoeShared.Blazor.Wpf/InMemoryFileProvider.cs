using DynamicData;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using PoeShared.IO;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf;

public sealed class InMemoryFileProvider : DisposableReactiveObjectWithLogger, IFileProvider
{
    private readonly ISourceCache<IFileInfo, OSPath> filesByName = new SourceCache<IFileInfo, OSPath>(x => new OSPath(x.Name));

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return NotFoundDirectoryContents.Singleton;
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        if (filesByName.TryGetValue(new OSPath(subpath), out var fileInfo))
        {
            return fileInfo;
        }
        return new NotFoundFileInfo(subpath);
    }

    public IChangeToken Watch(string filter)
    {
        return NullChangeToken.Singleton;
    }

    public ISourceCache<IFileInfo, OSPath> FilesByName => filesByName;
}