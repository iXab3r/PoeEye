using System;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace PoeShared.Blazor.Wpf;

public sealed class InMemoryFileInfo : IFileInfo
{
    private readonly byte[] fileBytes;

    public InMemoryFileInfo(string fileName, byte[] fileBytes, DateTimeOffset lastModified)
    {
        Name = fileName;
        this.fileBytes = fileBytes;
        LastModified = lastModified;
    }

    public Stream CreateReadStream()
    {
        return new MemoryStream(fileBytes);
    }

    public bool Exists => true;

    public bool IsDirectory => false;
    
    public DateTimeOffset LastModified { get; }
    public long Length => fileBytes.Length;
    public string Name { get; }
    public string PhysicalPath => null;
}