using System;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace PoeShared.Blazor.Scaffolding;

public sealed class RefFileInfo : IFileInfo
{
    public RefFileInfo(string filePath)
    {
        Name = filePath;
    }

    public Stream CreateReadStream()
    {
        throw new NotSupportedException();
    }

    public bool Exists => true;
    public bool IsDirectory => false;
    public DateTimeOffset LastModified => default;
    public long Length => default;

    public string Name { get; }

    public string PhysicalPath => null;
}