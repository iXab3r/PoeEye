using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using PoeShared.IO;
using PoeShared.Scaffolding;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Scaffolding;

/*
 PhysicalFileProvider behavior documented by this fixture:
 - Root lookup uses an empty subpath: GetDirectoryContents("") exists, while GetDirectoryContents(".") does not.
 - Leading '/' stays provider-relative: GetFileInfo("/root.txt") resolves, and GetDirectoryContents("/assets") matches "assets".
 - GetFileInfo is file-shaped only: GetFileInfo("root.txt") resolves, while GetFileInfo("assets") behaves like missing.
 - FileInfo.Name is always the leaf name: GetFileInfo("assets/nested/deep.txt").Name is "deep.txt", not "assets/nested/deep.txt".
 - GetDirectoryContents is non-recursive: GetDirectoryContents("assets") returns "app.js", "nested", "site.css", not "deep.txt".
 - Directory listings mix entry kinds: GetDirectoryContents("") returns files like "root.txt" and directories like "assets".
 - Directory entries are directory-shaped IFileInfo values: "assets" has Exists=true, IsDirectory=true, Length=-1.
 - Directory entries are not readable as files: CreateReadStream() on "assets" throws, while "root.txt" is readable.
 - In this runtime, directory entries are not IDirectoryContents: the "assets" entry cannot be cast, so callers re-query GetDirectoryContents("assets").
 - Missing lookups stay non-throwing: GetDirectoryContents("missing") and GetDirectoryContents("root.txt") both return Exists=false.
 - Encoded segments are treated literally: GetFileInfo("encoded/plus+space.txt") resolves, while "encoded/plus%2Bspace.txt" does not.
 - Traversal outside root is rejected: GetFileInfo("../escape.txt") is missing, and GetFiles("../escape", "*", recursive) is empty.
 - Recursive helpers preserve provider-relative paths: GetFiles("", "*.txt", recursive) returns "assets/nested/deep.txt", not just "deep.txt".
 - EnumerationOptions still matter: with Win32 matching "*.*" includes "docs/readme", while simple matching does not.
*/
[TestFixture]
public class PhysicalFileProviderFixture : FileProviderFixtureBase
{
    [Test]
    [TestCaseSource(nameof(ShouldDocumentGetFileInfoBehaviorCases))]
    public void ShouldDocumentGetFileInfoBehavior(string subpath, string what, string how, FileInfoSnapshot expected)
    {
        //Given
        Describe(what, how);

        //When
        var result = ToFileInfoSnapshot(PhysicalFileProvider.GetFileInfo(subpath));
        LogFileInfoState("Expected", expected);
        LogFileInfoState("Actual", result);

        //Then
        result.ShouldBe(expected);
    }

    [Test]
    [TestCaseSource(nameof(ShouldDocumentGetDirectoryContentsBehaviorCases))]
    public void ShouldDocumentGetDirectoryContentsBehavior(string subpath, string what, string how, DirectoryContentsSnapshot expected)
    {
        //Given
        Describe(what, how);

        //When
        var result = ToDirectoryContentsSnapshot(PhysicalFileProvider.GetDirectoryContents(subpath));
        LogDirectoryContentsState("Expected", expected);
        LogDirectoryContentsState("Actual", result);

        //Then
        result.Exists.ShouldBe(expected.Exists);
        result.Entries.ShouldBe(expected.Entries);
    }

    [Test]
    public void ShouldDocumentDirectoryEntryBehavior()
    {
        //Given
        Describe(
            what: "Directory entries returned by PhysicalFileProvider.GetDirectoryContents",
            how: "A child directory should be exposed as a directory-shaped IFileInfo, should not be streamable, and in this runtime it should be re-enumerated through GetDirectoryContents(path) instead of being self-enumerable.");

        //When
        var result = PhysicalFileProvider.GetDirectoryContents(string.Empty).Single(x => x.Name == "assets");
        var childNames = PhysicalFileProvider.GetDirectoryContents(result.Name)
            .Select(x => x.Name)
            .OrderBy(x => x, PathUtils.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .ToArray();

        Log.Debug($"Entry state: exists={result.Exists}, isDirectory={result.IsDirectory}, length={result.Length}, name={result.Name}, isDirectoryContents={result is IDirectoryContents}");
        LogPaths("Child entry names", childNames);

        //Then
        result.Exists.ShouldBe(true);
        result.IsDirectory.ShouldBe(true);
        result.Length.ShouldBe(-1);
        (result is IDirectoryContents).ShouldBe(false);
        Should.Throw<InvalidOperationException>(() => result.CreateReadStream());
        childNames.ShouldBe(new[] { "app.js", "nested", "site.css" });
    }

    [Test]
    [TestCaseSource(nameof(ShouldDocumentGetFilesBehaviorCases))]
    public void ShouldDocumentGetFilesBehavior(string searchPattern, bool recurseSubdirectories, MatchType matchType, string what, string how)
    {
        //Given
        Describe(what, how);
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = recurseSubdirectories,
            MatchType = matchType,
        };
        var expected = GetExpectedFilePaths(searchPattern, enumerationOptions);

        //When
        var result = PhysicalFileProvider
            .GetFiles(string.Empty, searchPattern, enumerationOptions)
            .Select(x => x.GetSubpath())
            .OrderBy(x => x, PathUtils.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .ToArray();

        LogPaths("Expected", expected);
        LogPaths("Actual", result);

        //Then
        result.ShouldBe(expected);
    }

    [Test]
    [TestCaseSource(nameof(ShouldDocumentGetDirectoriesBehaviorCases))]
    public void ShouldDocumentGetDirectoriesBehavior(string searchPattern, bool recurseSubdirectories, string what, string how)
    {
        //Given
        Describe(what, how);
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = recurseSubdirectories,
        };
        var expected = GetExpectedDirectoryPaths(searchPattern, enumerationOptions);

        //When
        var result = PhysicalFileProvider
            .GetDirectories(string.Empty, searchPattern, enumerationOptions)
            .Select(x => x.GetSubpath())
            .OrderBy(x => x, PathUtils.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .ToArray();

        LogPaths("Expected", expected);
        LogPaths("Actual", result);

        //Then
        result.ShouldBe(expected);
    }

    [Test]
    public void ShouldDocumentEncodedPathBehavior()
    {
        //Given
        Describe(
            what: "Encoded paths routed through PhysicalFileProvider.GetFileInfo",
            how: "PhysicalFileProvider should treat the supplied subpath literally, so already-encoded path segments such as '%2B' and '%20' should not be decoded automatically.");

        //When
        var rawPlusResult = PhysicalFileProvider.GetFileInfo("encoded/plus+space.txt");
        var encodedPlusResult = PhysicalFileProvider.GetFileInfo("encoded/plus%2Bspace.txt");
        var encodedSpaceResult = PhysicalFileProvider.GetFileInfo("encoded/space%20name.txt");

        LogFileInfoState("Raw plus", ToFileInfoSnapshot(rawPlusResult));
        LogFileInfoState("Encoded plus", ToFileInfoSnapshot(encodedPlusResult));
        LogFileInfoState("Encoded space", ToFileInfoSnapshot(encodedSpaceResult));

        //Then
        rawPlusResult.Exists.ShouldBe(true);
        encodedPlusResult.Exists.ShouldBe(false);
        encodedSpaceResult.Exists.ShouldBe(false);
        PhysicalFileProvider.ReadAllText("encoded/plus+space.txt").ShouldBe("plus+space");
    }

    [Test]
    public void ShouldDocumentTraversalBehavior()
    {
        //Given
        Describe(
            what: "Traversal-like paths against PhysicalFileProvider and the IFileProvider-based directory helpers",
            how: "Lookups outside the provider root should be rejected as missing, and enumerations should return empty collections instead of leaking parent directory content.");

        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
        };

        //When
        var fileResult = PhysicalFileProvider.GetFileInfo("../escape.txt");
        var directoryResult = PhysicalFileProvider.GetDirectoryContents("../escape");
        var files = PhysicalFileProvider.GetFiles("../escape", "*", enumerationOptions);
        var directories = PhysicalFileProvider.GetDirectories("../escape", "*", enumerationOptions);

        LogFileInfoState("Traversal file", ToFileInfoSnapshot(fileResult));
        LogDirectoryContentsState("Traversal directory", ToDirectoryContentsSnapshot(directoryResult));
        LogPaths("Traversal files", files.Select(x => x.GetSubpath()).ToArray());
        LogPaths("Traversal directories", directories.Select(x => x.GetSubpath()).ToArray());

        //Then
        fileResult.Exists.ShouldBe(false);
        directoryResult.Exists.ShouldBe(false);
        files.ShouldBeEmpty();
        directories.ShouldBeEmpty();
    }

    [Test]
    public void ShouldDocumentRecursivePathAwareGetFilesBehavior()
    {
        //Given
        Describe(
            what: "Recursive GetFiles paths produced from PhysicalFileProvider",
            how: "Recursive file enumeration should keep the full provider-relative subpath instead of collapsing results to leaf file names.");

        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
        };

        //When
        var result = PhysicalFileProvider
            .GetFiles(string.Empty, "*.txt", enumerationOptions)
            .Select(x => x.GetSubpath())
            .OrderBy(x => x, PathUtils.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .ToArray();

        LogPaths("Recursive txt files", result);

        //Then
        result.ShouldContain("assets/nested/deep.txt");
        result.ShouldContain("docs/readme.txt");
        result.ShouldContain("root.txt");
        result.ShouldNotContain("deep.txt");
        result.ShouldNotContain("readme.txt");
    }

    public static IEnumerable<NamedTestCaseData> ShouldDocumentGetFileInfoBehaviorCases()
    {
        yield return new NamedTestCaseData(
            "root.txt",
            "GetFileInfo for a root file",
            "A direct file lookup at the provider root should return a readable file entry with the leaf name and file content.",
            new FileInfoSnapshot(true, false, "root.txt", 4, "root")) { TestName = "root file" };

        yield return new NamedTestCaseData(
            "/root.txt",
            "GetFileInfo for a root file with a leading slash",
            "Leading slashes should still resolve relative to the provider root instead of being treated as absolute file-system paths.",
            new FileInfoSnapshot(true, false, "root.txt", 4, "root")) { TestName = "leading slash root file" };

        yield return new NamedTestCaseData(
            "assets/nested/deep.txt",
            "GetFileInfo for a nested file",
            "Nested file lookups should resolve the file, keep the leaf name, and expose the file contents through CreateReadStream().",
            new FileInfoSnapshot(true, false, "deep.txt", 4, "deep")) { TestName = "nested file" };

        yield return new NamedTestCaseData(
            "/assets/nested/deep.txt",
            "GetFileInfo for a nested file with a leading slash",
            "Nested paths with a leading slash should still resolve against the provider root and return the same file-shaped entry.",
            new FileInfoSnapshot(true, false, "deep.txt", 4, "deep")) { TestName = "leading slash nested file" };

        yield return new NamedTestCaseData(
            "assets",
            "GetFileInfo for a directory path",
            "Directory paths should not be exposed as files through GetFileInfo; callers are expected to use GetDirectoryContents instead.",
            new FileInfoSnapshot(false, false, "assets", null, null)) { TestName = "directory behaves like missing file" };

        yield return new NamedTestCaseData(
            ".",
            "GetFileInfo for the dot root path",
            "The dot root path should not resolve to a file entry even though it conceptually points at the provider root.",
            new FileInfoSnapshot(false, false, ".", null, null)) { TestName = "dot root path" };

        yield return new NamedTestCaseData(
            "missing.txt",
            "GetFileInfo for a missing file",
            "A missing file lookup should return a non-existing file-shaped result with the missing leaf name preserved for diagnostics.",
            new FileInfoSnapshot(false, false, "missing.txt", null, null)) { TestName = "missing file" };
    }

    public static IEnumerable<NamedTestCaseData> ShouldDocumentGetDirectoryContentsBehaviorCases()
    {
        yield return new NamedTestCaseData(
            string.Empty,
            "GetDirectoryContents for the provider root",
            "Directory enumeration should return only the immediate children of the requested directory, mixing files and subdirectories in one listing.",
            new DirectoryContentsSnapshot(
                true,
                new[]
                {
                    new DirectoryEntrySnapshot("a+b.txt", false, true, 4),
                    new DirectoryEntrySnapshot("assets", true, true, -1),
                    new DirectoryEntrySnapshot("docs", true, true, -1),
                    new DirectoryEntrySnapshot("encoded", true, true, -1),
                    new DirectoryEntrySnapshot("root.txt", false, true, 4),
                    new DirectoryEntrySnapshot("scripts", true, true, -1),
                })) { TestName = "root" };

        yield return new NamedTestCaseData(
            ".",
            "GetDirectoryContents for the dot root path",
            "The dot path should not be treated as a directory alias; callers must use an empty subpath to enumerate the provider root.",
            new DirectoryContentsSnapshot(false, Array.Empty<DirectoryEntrySnapshot>())) { TestName = "dot root" };

        yield return new NamedTestCaseData(
            "assets",
            "GetDirectoryContents for a nested directory",
            "Nested directory enumeration should return only the direct children of that directory and should include child directories as directory-shaped entries.",
            new DirectoryContentsSnapshot(
                true,
                new[]
                {
                    new DirectoryEntrySnapshot("app.js", false, true, 3),
                    new DirectoryEntrySnapshot("nested", true, true, -1),
                    new DirectoryEntrySnapshot("site.css", false, true, 4),
                })) { TestName = "nested directory" };

        yield return new NamedTestCaseData(
            "/assets",
            "GetDirectoryContents for a nested directory with a leading slash",
            "Leading slashes should still resolve relative to the provider root and produce the same direct-child listing as the non-prefixed path.",
            new DirectoryContentsSnapshot(
                true,
                new[]
                {
                    new DirectoryEntrySnapshot("app.js", false, true, 3),
                    new DirectoryEntrySnapshot("nested", true, true, -1),
                    new DirectoryEntrySnapshot("site.css", false, true, 4),
                })) { TestName = "leading slash nested directory" };

        yield return new NamedTestCaseData(
            "assets/nested",
            "GetDirectoryContents for a deep nested directory",
            "Deep directory enumeration should stay non-recursive and surface only the immediate leaf files inside that directory.",
            new DirectoryContentsSnapshot(
                true,
                new[]
                {
                    new DirectoryEntrySnapshot("deep.txt", false, true, 4),
                    new DirectoryEntrySnapshot("LICENSE", false, true, 7),
                })) { TestName = "deep nested directory" };

        yield return new NamedTestCaseData(
            "root.txt",
            "GetDirectoryContents for a file path",
            "Querying a file path as a directory should return a non-existing directory listing instead of throwing.",
            new DirectoryContentsSnapshot(false, Array.Empty<DirectoryEntrySnapshot>())) { TestName = "file path" };

        yield return new NamedTestCaseData(
            "missing",
            "GetDirectoryContents for a missing directory",
            "A missing directory lookup should return a non-existing directory listing instead of an exception or a fabricated empty directory.",
            new DirectoryContentsSnapshot(false, Array.Empty<DirectoryEntrySnapshot>())) { TestName = "missing directory" };
    }

    public static IEnumerable<NamedTestCaseData> ShouldDocumentGetFilesBehaviorCases()
    {
        yield return new NamedTestCaseData(
            "*.txt",
            false,
            MatchType.Simple,
            "GetFiles for top-level txt files",
            "Without recursion, GetFiles should only return matching files from the requested directory and should ignore descendants from nested directories.") { TestName = "top directory text files" };

        yield return new NamedTestCaseData(
            "*.txt",
            true,
            MatchType.Simple,
            "GetFiles for recursive txt files",
            "With recursion enabled, GetFiles should walk the full subtree and preserve provider-relative subpaths for each matching file.") { TestName = "recursive text files" };

        yield return new NamedTestCaseData(
            "*.*",
            true,
            MatchType.Simple,
            "GetFiles using simple wildcard matching",
            "Simple wildcard matching should keep the BCL behavior where '*.*' excludes extensionless files such as 'readme' and 'LICENSE'.") { TestName = "simple wildcard excludes extensionless files" };

        yield return new NamedTestCaseData(
            "*.*",
            true,
            MatchType.Win32,
            "GetFiles using Win32 wildcard matching",
            "Win32 wildcard matching should align with DirectoryInfo and include extensionless files when '*.*' is used.") { TestName = "win32 wildcard includes extensionless files" };
    }

    public static IEnumerable<NamedTestCaseData> ShouldDocumentGetDirectoriesBehaviorCases()
    {
        yield return new NamedTestCaseData(
            "*",
            false,
            "GetDirectories for top-level directories",
            "Without recursion, GetDirectories should return only direct child directories of the requested provider path.") { TestName = "top directory only" };

        yield return new NamedTestCaseData(
            "*",
            true,
            "GetDirectories for recursive enumeration",
            "With recursion enabled, GetDirectories should traverse the full subtree and return provider-relative subpaths for each directory.") { TestName = "recursive" };

        yield return new NamedTestCaseData(
            "a*",
            true,
            "GetDirectories with a filtered pattern",
            "Pattern filtering should be applied to directory names while still traversing recursively when recursion is enabled.") { TestName = "pattern filtered recursive" };
    }

    private void Describe(string what, string how)
    {
        Log.Info($"WHAT: {what}");
        Log.Info($"HOW: {how}");
        Log.Debug($"Provider root: {TempDirectory.FullName}");
    }

    private void LogPaths(string caption, IEnumerable<string> values)
    {
        Log.Debug($"{caption}: [{string.Join(", ", values ?? Array.Empty<string>())}]");
    }

    private void LogFileInfoState(string caption, FileInfoSnapshot snapshot)
    {
        Log.Debug($"{caption}: exists={snapshot.Exists}, isDirectory={snapshot.IsDirectory}, name={snapshot.Name}, length={snapshot.Length}, content={snapshot.Content}");
    }

    private void LogDirectoryContentsState(string caption, DirectoryContentsSnapshot snapshot)
    {
        var entries = snapshot.Entries.Select(x => $"{x.Name}|dir={x.IsDirectory}|exists={x.Exists}|len={x.Length}").ToArray();
        Log.Debug($"{caption}: exists={snapshot.Exists}, entries=[{string.Join(", ", entries)}]");
    }

    private static DirectoryContentsSnapshot ToDirectoryContentsSnapshot(IDirectoryContents directoryContents)
    {
        if (directoryContents == null || !directoryContents.Exists)
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

    public sealed record FileInfoSnapshot(bool Exists, bool IsDirectory, string Name, long? Length, string Content);

    public sealed record DirectoryContentsSnapshot(bool Exists, DirectoryEntrySnapshot[] Entries);

    public sealed record DirectoryEntrySnapshot(string Name, bool IsDirectory, bool Exists, long? Length);
}
