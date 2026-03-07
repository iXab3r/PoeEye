using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class FileProviderExtensionsFixture
{
    [Test]
    public void ShouldSkipMalformedDirectories()
    {
        //Given
        var fileProvider = new BrokenDirectoryFileProvider();

        //When
        var files = fileProvider.GetFiles(string.Empty, "*", SearchOption.AllDirectories).Select(x => x.GetSubpath()).ToArray();
        var directories = fileProvider.GetDirectories(string.Empty, "*", SearchOption.AllDirectories).Select(x => x.GetSubpath()).ToArray();

        //Then
        files.ShouldBe(new[] { "visible.txt" });
        directories.ShouldBeEmpty();
    }

    private sealed class BrokenDirectoryFileProvider : IFileProvider
    {
        private readonly IFileInfo visibleFile = new InMemoryFileInfo("visible.txt", Encoding.UTF8.GetBytes("visible"), new DateTimeOffset(2024, 7, 28, 0, 0, 0, TimeSpan.Zero));
        private readonly IDirectoryContents rootContents;

        public BrokenDirectoryFileProvider()
        {
            rootContents = new TestDirectoryContents(
                visibleFile,
                new TestDirectoryInfo("ghost"));
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            if (string.IsNullOrEmpty(subpath) || subpath == ".")
            {
                return rootContents;
            }

            return NotFoundDirectoryContents.Singleton;
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            return string.Equals(subpath, "visible.txt", StringComparison.OrdinalIgnoreCase)
                ? visibleFile
                : new NotFoundFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            return NullChangeToken.Singleton;
        }
    }

    private sealed class TestDirectoryContents : IDirectoryContents
    {
        private readonly IFileInfo[] entries;

        public TestDirectoryContents(params IFileInfo[] entries)
        {
            this.entries = entries ?? Array.Empty<IFileInfo>();
        }

        public bool Exists => true;

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            return ((IEnumerable<IFileInfo>) entries).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private sealed class TestDirectoryInfo : IFileInfo
    {
        public TestDirectoryInfo(string name)
        {
            Name = name;
        }

        public Stream CreateReadStream()
        {
            throw new InvalidOperationException("Directory entries are not streamable.");
        }

        public bool Exists => true;

        public bool IsDirectory => true;

        public DateTimeOffset LastModified => new DateTimeOffset(2024, 7, 28, 0, 0, 0, TimeSpan.Zero);

        public long Length => -1;

        public string Name { get; }

        public string PhysicalPath => null;
    }
}
