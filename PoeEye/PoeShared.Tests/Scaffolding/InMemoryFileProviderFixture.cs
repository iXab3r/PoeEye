using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DynamicData;
using Microsoft.Extensions.FileProviders;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class InMemoryFileProviderFixture : FileProviderFixtureBase
{
    [Test]
    [TestCaseSource(nameof(ShouldMatchPhysicalFileProviderForGetFileInfoCases))]
    public void ShouldMatchPhysicalFileProviderForGetFileInfo(string subpath)
    {
        //Given
        //When
        var expected = ToFileInfoSnapshot(PhysicalFileProvider.GetFileInfo(subpath));
        var result = ToFileInfoSnapshot(InMemoryFileProvider.GetFileInfo(subpath));

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
        var result = ToDirectoryContentsSnapshot(InMemoryFileProvider.GetDirectoryContents(subpath));

        //Then
        result.Exists.ShouldBe(expected.Exists);
        result.Entries.ShouldBe(expected.Entries);
    }

    [Test]
    public void ShouldReturnImmediateChildrenOnly()
    {
        //Given
        //When
        var result = InMemoryFileProvider.GetDirectoryContents(string.Empty)
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
        var result = InMemoryFileProvider.GetDirectoryContents(string.Empty).Single(x => x.Name == "assets");

        //Then
        result.IsDirectory.ShouldBe(true);
        result.Exists.ShouldBe(expected.Exists);
        result.Length.ShouldBe(expected.Length);
        (result is IDirectoryContents).ShouldBe(expected is IDirectoryContents);
        Should.Throw<InvalidOperationException>(() => result.CreateReadStream());
        InMemoryFileProvider.GetDirectoryContents(result.Name).Select(x => x.Name).OrderBy(x => x, comparer).ToArray()
            .ShouldBe(PhysicalFileProvider.GetDirectoryContents(expected.Name).Select(x => x.Name).OrderBy(x => x, comparer).ToArray());
    }

    [Test]
    public void ShouldSupportExplicitEmptyDirectoryEntries()
    {
        //Given
        var instance = new InMemoryFileProvider();
        instance.FilesByName.AddOrUpdate(new DirectoryFileInfoStub("generated/empty", new DateTimeOffset(2024, 7, 28, 0, 0, 0, TimeSpan.Zero)));

        //When
        var rootContents = instance.GetDirectoryContents(string.Empty).ToArray();
        var generatedContents = instance.GetDirectoryContents("generated").ToArray();
        var emptyContents = instance.GetDirectoryContents("generated/empty").ToArray();

        //Then
        rootContents.Select(x => x.Name).ShouldBe(new[] { "generated" });
        generatedContents.Select(x => x.Name).ShouldBe(new[] { "empty" });
        instance.GetFileInfo("generated").Exists.ShouldBe(false);
        instance.GetFileInfo("generated/empty").Exists.ShouldBe(false);
        emptyContents.ShouldBeEmpty();
        instance.GetDirectoryContents("generated/empty").Exists.ShouldBe(true);
    }

    [Test]
    public void ShouldRefreshSnapshotAfterFilesByNameMutation()
    {
        //Given
        InMemoryFileProvider.GetDirectoryContents(string.Empty).Exists.ShouldBe(true);

        //When
        InMemoryFileProvider.FilesByName.AddOrUpdate(new InMemoryFileInfo("later/created.txt", System.Text.Encoding.UTF8.GetBytes("created"), new DateTimeOffset(2024, 7, 28, 0, 0, 0, TimeSpan.Zero)));

        //Then
        InMemoryFileProvider.GetFileInfo("later/created.txt").Exists.ShouldBe(true);
        InMemoryFileProvider.GetDirectoryContents("later").Select(x => x.Name).ShouldBe(new[] { "created.txt" });
    }

    [Test]
    public void ShouldResolveEncodedPathsWithoutTreatingPlusAsSpace()
    {
        //Given
        //When
        var rawPlusResult = InMemoryFileProvider.GetFileInfo("encoded/plus+space.txt");
        var encodedPlusResult = InMemoryFileProvider.GetFileInfo("encoded/plus%2Bspace.txt");
        var encodedSpaceResult = InMemoryFileProvider.GetFileInfo("encoded/space%20name.txt");

        //Then
        rawPlusResult.Exists.ShouldBe(true);
        encodedPlusResult.Exists.ShouldBe(true);
        encodedSpaceResult.Exists.ShouldBe(true);
        InMemoryFileProvider.ReadAllText("encoded/plus+space.txt").ShouldBe("plus+space");
        InMemoryFileProvider.ReadAllText("encoded/space%20name.txt").ShouldBe("space name");
    }

    [Test]
    public void ShouldRejectPathTraversalLookups()
    {
        //Given
        //When
        var fileResult = InMemoryFileProvider.GetFileInfo("../escape.txt");
        var directoryResult = InMemoryFileProvider.GetDirectoryContents("../escape");

        //Then
        fileResult.Exists.ShouldBe(false);
        directoryResult.Exists.ShouldBe(false);
    }

    [Test]
    public void ShouldTreatRootPathAsMissingFile()
    {
        //Given
        //When
        var result = InMemoryFileProvider.GetFileInfo(".");

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

    private sealed class DirectoryFileInfoStub : IFileInfo
    {
        public DirectoryFileInfoStub(string name, DateTimeOffset lastModified)
        {
            Name = name;
            LastModified = lastModified;
        }

        public Stream CreateReadStream()
        {
            throw new InvalidOperationException("Directory entries are not streamable.");
        }

        public bool Exists => true;

        public bool IsDirectory => true;

        public DateTimeOffset LastModified { get; }

        public long Length => -1;

        public string Name { get; }

        public string PhysicalPath => null;
    }
}
