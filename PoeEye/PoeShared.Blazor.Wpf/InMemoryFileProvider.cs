using DynamicData;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf;

public sealed class InMemoryFileProvider : DisposableReactiveObjectWithLogger, IFileProvider
{
    private readonly ISourceCache<IFileInfo, string> filesByName = new SourceCache<IFileInfo, string>(x => x.Name);

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return NotFoundDirectoryContents.Singleton;
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        if (filesByName.TryGetValue(subpath, out var fileInfo))
        {
            return fileInfo;
        }
        return new NotFoundFileInfo(subpath);
    }

    public IChangeToken Watch(string filter)
    {
        return NullChangeToken.Singleton;
    }

    public void AddFile(IFileInfo fileInfo)
    {
        filesByName.AddOrUpdate(fileInfo);
    }
}