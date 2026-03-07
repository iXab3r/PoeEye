using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class FileProviderExtensionsFixture : FileProviderFixtureBase
{
    [Test]
    [TestCaseSource(nameof(ShouldGetFilesCases))]
    public void ShouldGetFilesLikeDirectoryInfo(string searchPattern, bool recurseSubdirectories, MatchType matchType)
    {
        //Given
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = recurseSubdirectories,
            MatchType = matchType,
        };
        var expected = GetExpectedFilePaths(searchPattern, enumerationOptions);
        var comparer = PathUtils.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        //When
        var physicalResult = PhysicalFileProvider.GetFiles(string.Empty, searchPattern, enumerationOptions).Select(x => x.GetSubpath()).OrderBy(x => x, comparer).ToArray();
        var inMemoryResult = InMemoryFileProvider.GetFiles(string.Empty, searchPattern, enumerationOptions).Select(x => x.GetSubpath()).OrderBy(x => x, comparer).ToArray();

        //Then
        physicalResult.ShouldBe(expected);
        inMemoryResult.ShouldBe(expected);
    }

    [Test]
    [TestCaseSource(nameof(ShouldGetDirectoriesCases))]
    public void ShouldGetDirectoriesLikeDirectoryInfo(string searchPattern, bool recurseSubdirectories)
    {
        //Given
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = recurseSubdirectories,
        };
        var expected = GetExpectedDirectoryPaths(searchPattern, enumerationOptions);
        var comparer = PathUtils.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        //When
        var physicalResult = PhysicalFileProvider.GetDirectories(string.Empty, searchPattern, enumerationOptions).Select(x => x.GetSubpath()).OrderBy(x => x, comparer).ToArray();
        var inMemoryResult = InMemoryFileProvider.GetDirectories(string.Empty, searchPattern, enumerationOptions).Select(x => x.GetSubpath()).OrderBy(x => x, comparer).ToArray();

        //Then
        physicalResult.ShouldBe(expected);
        inMemoryResult.ShouldBe(expected);
    }

    [Test]
    public void ShouldReturnPathAwareResultsForRecursiveFiles()
    {
        //Given
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
        };
        var comparer = PathUtils.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        //When
        var result = InMemoryFileProvider.GetFiles(string.Empty, "*.txt", enumerationOptions).Select(x => x.GetSubpath()).OrderBy(x => x, comparer).ToArray();

        //Then
        result.ShouldContain("assets/nested/deep.txt");
        result.ShouldContain("docs/readme.txt");
        result.ShouldContain("root.txt");
        result.ShouldNotContain("deep.txt");
        result.ShouldNotContain("readme.txt");
    }

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

    public static IEnumerable<NamedTestCaseData> ShouldGetFilesCases()
    {
        yield return new NamedTestCaseData("*.txt", false, MatchType.Simple) { TestName = "top directory text files" };
        yield return new NamedTestCaseData("*.txt", true, MatchType.Simple) { TestName = "recursive text files" };
        yield return new NamedTestCaseData("*.*", true, MatchType.Simple) { TestName = "simple wildcard excludes extensionless files" };
        yield return new NamedTestCaseData("*.*", true, MatchType.Win32) { TestName = "win32 wildcard includes extensionless files" };
    }

    public static IEnumerable<NamedTestCaseData> ShouldGetDirectoriesCases()
    {
        yield return new NamedTestCaseData("*", false) { TestName = "top directory only" };
        yield return new NamedTestCaseData("*", true) { TestName = "recursive" };
        yield return new NamedTestCaseData("a*", true) { TestName = "pattern filtered recursive" };
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
