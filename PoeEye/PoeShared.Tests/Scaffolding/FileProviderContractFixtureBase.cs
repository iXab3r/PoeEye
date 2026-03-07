using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using PoeShared.IO;
using PoeShared.Scaffolding;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Scaffolding;

public abstract class FileProviderContractFixtureBase : FileProviderFixtureBase
{
    protected abstract IFileProvider ProviderUnderTest { get; }

    [Test]
    [TestCaseSource(nameof(ShouldMatchPhysicalFileProviderForGetFileInfoCases))]
    public void ShouldMatchPhysicalFileProviderForGetFileInfo(string subpath)
    {
        //Given
        //When
        var expected = ToFileInfoSnapshot(PhysicalFileProvider.GetFileInfo(subpath));
        var result = ToFileInfoSnapshot(ProviderUnderTest.GetFileInfo(subpath));

        //Then
        result.ShouldBe(expected);
    }

    [Test]
    [TestCaseSource(nameof(ShouldMatchPhysicalFileProviderForDirectoryContentsCases))]
    public void ShouldMatchPhysicalFileProviderForDirectoryContents(string subpath)
    {
        //Given
        //When
        var expected = ToDirectoryContentsSnapshot(PhysicalFileProvider.GetDirectoryContents(subpath));
        var result = ToDirectoryContentsSnapshot(ProviderUnderTest.GetDirectoryContents(subpath));

        //Then
        result.Exists.ShouldBe(expected.Exists);
        result.Entries.ShouldBe(expected.Entries);
    }

    [Test]
    public void ShouldReturnImmediateChildrenOnly()
    {
        //Given
        //When
        var result = ProviderUnderTest.GetDirectoryContents(string.Empty)
            .Select(x => x.Name)
            .OrderBy(x => x, PathUtils.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .ToArray();

        //Then
        result.ShouldBe(new[]
        {
            "a+b.txt",
            "assets",
            "docs",
            "encoded",
            "root.txt",
            "scripts",
        });
    }

    [Test]
    public void ShouldExposeDirectoryEntriesLikePhysicalProvider()
    {
        //Given
        var expected = PhysicalFileProvider.GetDirectoryContents(string.Empty).Single(x => x.Name == "assets");
        var comparer = PathUtils.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        //When
        var result = ProviderUnderTest.GetDirectoryContents(string.Empty).Single(x => x.Name == "assets");

        //Then
        result.IsDirectory.ShouldBe(true);
        result.Exists.ShouldBe(expected.Exists);
        result.Length.ShouldBe(expected.Length);
        (result is IDirectoryContents).ShouldBe(expected is IDirectoryContents);
        Should.Throw<InvalidOperationException>(() => result.CreateReadStream());
        ProviderUnderTest.GetDirectoryContents(result.Name).Select(x => x.Name).OrderBy(x => x, comparer).ToArray()
            .ShouldBe(PhysicalFileProvider.GetDirectoryContents(expected.Name).Select(x => x.Name).OrderBy(x => x, comparer).ToArray());
    }

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
        var result = ProviderUnderTest.GetFiles(string.Empty, searchPattern, enumerationOptions).Select(x => x.GetSubpath()).OrderBy(x => x, comparer).ToArray();

        //Then
        physicalResult.ShouldBe(expected);
        result.ShouldBe(expected);
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
        var result = ProviderUnderTest.GetDirectories(string.Empty, searchPattern, enumerationOptions).Select(x => x.GetSubpath()).OrderBy(x => x, comparer).ToArray();

        //Then
        physicalResult.ShouldBe(expected);
        result.ShouldBe(expected);
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
        var result = ProviderUnderTest.GetFiles(string.Empty, "*.txt", enumerationOptions).Select(x => x.GetSubpath()).OrderBy(x => x, comparer).ToArray();

        //Then
        result.ShouldContain("assets/nested/deep.txt");
        result.ShouldContain("docs/readme.txt");
        result.ShouldContain("root.txt");
        result.ShouldNotContain("deep.txt");
        result.ShouldNotContain("readme.txt");
    }

    [Test]
    public void ShouldResolveEncodedPathsWithoutTreatingPlusAsSpace()
    {
        //Given
        //When
        var rawPlusResult = ProviderUnderTest.GetFileInfo("encoded/plus+space.txt");
        var encodedPlusResult = ProviderUnderTest.GetFileInfo("encoded/plus%2Bspace.txt");
        var encodedSpaceResult = ProviderUnderTest.GetFileInfo("encoded/space%20name.txt");

        //Then
        rawPlusResult.Exists.ShouldBe(true);
        encodedPlusResult.Exists.ShouldBe(true);
        encodedSpaceResult.Exists.ShouldBe(true);
        ProviderUnderTest.ReadAllText("encoded/plus+space.txt").ShouldBe("plus+space");
        ProviderUnderTest.ReadAllText("encoded/space%20name.txt").ShouldBe("space name");
    }

    [Test]
    public void ShouldRejectPathTraversalLookups()
    {
        //Given
        //When
        var fileResult = ProviderUnderTest.GetFileInfo("../escape.txt");
        var directoryResult = ProviderUnderTest.GetDirectoryContents("../escape");

        //Then
        fileResult.Exists.ShouldBe(false);
        directoryResult.Exists.ShouldBe(false);
    }

    [Test]
    public void ShouldTreatRootPathAsMissingFile()
    {
        //Given
        //When
        var result = ProviderUnderTest.GetFileInfo(".");

        //Then
        result.Exists.ShouldBe(false);
        result.IsDirectory.ShouldBe(false);
    }

    public static IEnumerable<NamedTestCaseData> ShouldMatchPhysicalFileProviderForGetFileInfoCases()
    {
        yield return new NamedTestCaseData("root.txt") { TestName = "root file" };
        yield return new NamedTestCaseData("/root.txt") { TestName = "leading slash root file" };
        yield return new NamedTestCaseData("assets/nested/deep.txt") { TestName = "nested file" };
        yield return new NamedTestCaseData("/assets/nested/deep.txt") { TestName = "leading slash nested file" };
        yield return new NamedTestCaseData("assets") { TestName = "directory path behaves like missing file" };
        yield return new NamedTestCaseData("missing.txt") { TestName = "missing file" };
    }

    public static IEnumerable<NamedTestCaseData> ShouldMatchPhysicalFileProviderForDirectoryContentsCases()
    {
        yield return new NamedTestCaseData(string.Empty) { TestName = "root" };
        yield return new NamedTestCaseData(".") { TestName = "dot root" };
        yield return new NamedTestCaseData("assets") { TestName = "nested directory" };
        yield return new NamedTestCaseData("/assets") { TestName = "leading slash nested directory" };
        yield return new NamedTestCaseData("assets/nested") { TestName = "deep nested directory" };
        yield return new NamedTestCaseData("root.txt") { TestName = "file path" };
        yield return new NamedTestCaseData("missing") { TestName = "missing directory" };
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

    private static DirectoryContentsSnapshot ToDirectoryContentsSnapshot(IDirectoryContents directoryContents)
    {
        if (directoryContents == null)
        {
            return new DirectoryContentsSnapshot(false, Array.Empty<DirectoryEntrySnapshot>());
        }

        if (!directoryContents.Exists)
        {
            return new DirectoryContentsSnapshot(false, Array.Empty<DirectoryEntrySnapshot>());
        }

        return new DirectoryContentsSnapshot(
            true,
            directoryContents
                .Select(ToDirectoryEntrySnapshot)
                .OrderBy(x => x.Name, PathUtils.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                .ToArray());
    }

    private static FileInfoSnapshot ToFileInfoSnapshot(IFileInfo fileInfo)
    {
        if (fileInfo == null)
        {
            return new FileInfoSnapshot(false, false, string.Empty, null, null);
        }

        var exists = Safe(() => fileInfo.Exists);
        var isDirectory = Safe(() => fileInfo.IsDirectory);
        var name = Safe(() => fileInfo.Name) ?? string.Empty;

        return exists && !isDirectory
            ? new FileInfoSnapshot(true, false, name, Safe(() => fileInfo.Length), ReadAllText(fileInfo))
            : new FileInfoSnapshot(exists, isDirectory, name, null, null);
    }

    private static DirectoryEntrySnapshot ToDirectoryEntrySnapshot(IFileInfo fileInfo)
    {
        var isDirectory = Safe(() => fileInfo.IsDirectory);
        return new DirectoryEntrySnapshot(
            Safe(() => fileInfo.Name) ?? string.Empty,
            isDirectory,
            Safe(() => fileInfo.Exists),
            isDirectory ? -1 : Safe(() => fileInfo.Length));
    }

    private static string ReadAllText(IFileInfo fileInfo)
    {
        using var stream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static T Safe<T>(Func<T> getter)
    {
        try
        {
            return getter();
        }
        catch
        {
            return default;
        }
    }

    private sealed record FileInfoSnapshot(bool Exists, bool IsDirectory, string Name, long? Length, string Content);

    private sealed record DirectoryContentsSnapshot(bool Exists, DirectoryEntrySnapshot[] Entries);

    private sealed record DirectoryEntrySnapshot(string Name, bool IsDirectory, bool Exists, long? Length);
}
