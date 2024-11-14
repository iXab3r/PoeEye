using System;
using System.IO;
using ByteSizeLib;
using Microsoft.Extensions.FileProviders;

namespace PoeShared.Blazor.Wpf;

public sealed class LazyInMemoryFileInfo : IFileInfo
{
    private readonly Lazy<byte[]> fileBytesSupplier;
    
    public LazyInMemoryFileInfo(string fileName, Func<byte[]> fileBytesGetter, DateTimeOffset lastModified)
    {
        Name = fileName;
        fileBytesSupplier = new Lazy<byte[]>(() =>
        {
            var bytes = fileBytesGetter();
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(fileBytesGetter), $"Resolved null for resource {fileName}");
            }

            return bytes;
        });
        LastModified = lastModified;
    }

    public Stream CreateReadStream()
    {
        return new MemoryStream(fileBytesSupplier.Value);
    }

    public bool Exists => true;

    public bool IsDirectory => false;
    
    public DateTimeOffset LastModified { get; }
    
    public long Length => fileBytesSupplier.Value.Length;
    
    public string Name { get; }
    
    public string PhysicalPath => null;

    public override string ToString()
    {
        return !fileBytesSupplier.IsValueCreated ? $"InMemoryFile(resolved): {Name} ({ByteSize.FromBytes(Length)})" : $"InMemoryFile(unresolved): {Name}";
    }
}