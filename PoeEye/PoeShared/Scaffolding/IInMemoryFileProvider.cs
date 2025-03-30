using DynamicData;
using Microsoft.Extensions.FileProviders;
using PoeShared.IO;

namespace PoeShared.Scaffolding;

public interface IInMemoryFileProvider : IFileProvider, ISourceCache<IFileInfo, OSPath>
{
    /// <summary>
    /// Provides a reactive source of files stored in memory.
    /// </summary>
    ISourceCache<IFileInfo, OSPath> FilesByName { get; }
}