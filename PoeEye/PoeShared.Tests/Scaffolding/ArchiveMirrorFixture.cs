using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class ArchiveMirrorFixture : FixtureBase
{
    private static readonly DirectoryInfo AssetsDirectory = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scaffolding", "Assets", nameof(ArchiveMirrorFixture)));

    /// <summary>
    /// WHAT: Verifies that plain archive extraction preserves unrelated destination files.
    /// HOW: Uses the fixed valid ZIP asset, seeds a stale file in a temporary directory, extracts the archive, and asserts that both the archive content and the stale file remain.
    /// </summary>
    [Test]
    public void ShouldExtractZipWithoutCleaningDestination()
    {
        // Given
        using var destinationDirectory = new TemporaryDirectory();
        var archive = GetAssetFile("archive-valid.zip");
        var expectedFilePath = Path.Combine(destinationDirectory.Path, "payload", "app.txt");
        var staleFilePath = Path.Combine(destinationDirectory.Path, "stale.txt");

        File.WriteAllText(staleFilePath, "stale");

        // When
        FileUtils.ExtractArchive(archive, new DirectoryInfo(destinationDirectory.Path));

        // Then
        File.Exists(expectedFilePath).ShouldBeTrue();
        File.ReadAllText(expectedFilePath).ShouldBe("payload");
        File.Exists(staleFilePath).ShouldBeTrue();
        File.Exists(Path.Combine(destinationDirectory.Path, ".mirrored-archive")).ShouldBeFalse();
    }

    /// <summary>
    /// WHAT: Verifies that archive mirroring materializes the asset contents into an empty destination.
    /// HOW: Uses the fixed valid ZIP asset, mirrors it into a fresh temporary directory, and asserts that the expected files and completion marker are present.
    /// </summary>
    [Test]
    public void ShouldMirrorZipIntoEmptyDirectory()
    {
        // Given
        using var destinationDirectory = new TemporaryDirectory();
        var archive = GetAssetFile("archive-valid.zip");

        // When
        FileUtils.MirrorArchive(archive, new DirectoryInfo(destinationDirectory.Path));

        // Then
        File.ReadAllText(Path.Combine(destinationDirectory.Path, "root.txt")).ShouldBe("root");
        File.ReadAllText(Path.Combine(destinationDirectory.Path, "nested", "child.txt")).ShouldBe("child");
        File.ReadAllText(Path.Combine(destinationDirectory.Path, ".mirrored-archive")).ShouldBe("ok");
    }

    /// <summary>
    /// WHAT: Verifies that archive mirroring removes stale destination files that are not part of the archive.
    /// HOW: Seeds extra files in a temporary destination, mirrors the fixed valid ZIP asset into it, and asserts that stale files are removed while archive content remains.
    /// </summary>
    [Test]
    public void ShouldRemoveStaleFilesWhenMirroringExistingDestination()
    {
        // Given
        using var destinationDirectory = new TemporaryDirectory();
        var archive = GetAssetFile("archive-valid.zip");
        var staleFilePath = Path.Combine(destinationDirectory.Path, "stale.txt");
        var staleNestedPath = Path.Combine(destinationDirectory.Path, "nested", "old.txt");

        Directory.CreateDirectory(Path.GetDirectoryName(staleNestedPath)!);
        File.WriteAllText(staleFilePath, "stale");
        File.WriteAllText(staleNestedPath, "old");

        // When
        FileUtils.MirrorArchive(archive, new DirectoryInfo(destinationDirectory.Path));

        // Then
        File.Exists(staleFilePath).ShouldBeFalse();
        File.Exists(staleNestedPath).ShouldBeFalse();
        File.ReadAllText(Path.Combine(destinationDirectory.Path, "payload", "app.txt")).ShouldBe("payload");
        File.ReadAllText(Path.Combine(destinationDirectory.Path, ".mirrored-archive")).ShouldBe("ok");
    }

    /// <summary>
    /// WHAT: Verifies that traversal-style ZIP entries are rejected before extraction begins.
    /// HOW: Uses the fixed traversal ZIP asset and asserts that the mirror operation throws <see cref="InvalidDataException"/>.
    /// </summary>
    [Test]
    public void ShouldRejectTraversalEntries()
    {
        // Given
        using var destinationDirectory = new TemporaryDirectory();
        var archive = GetAssetFile("archive-traversal.zip");

        // When
        Action action = () => FileUtils.MirrorArchive(archive, new DirectoryInfo(destinationDirectory.Path));

        // Then
        action.ShouldThrow<InvalidDataException>();
    }

    /// <summary>
    /// WHAT: Verifies that mirroring the same archive twice produces a stable destination layout.
    /// HOW: Mirrors the fixed valid ZIP asset into the same temporary directory twice, captures the file snapshot after each pass, and compares both phases.
    /// </summary>
    [Test]
    public void ShouldRemainStableWhenMirroringSameArchiveTwice()
    {
        // Given
        using var destinationDirectory = new TemporaryDirectory();
        var archive = GetAssetFile("archive-valid.zip");

        var destination = new DirectoryInfo(destinationDirectory.Path);

        // When
        FileUtils.MirrorArchive(archive, destination);
        var firstPassFiles = ReadRelativeFileSnapshot(destinationDirectory.Path);

        // Then
        firstPassFiles.ShouldContain("root.txt");
        firstPassFiles.ShouldContain(Path.Combine("nested", "child.txt"));
        firstPassFiles.ShouldContain(Path.Combine("payload", "app.txt"));
        firstPassFiles.ShouldContain(".mirrored-archive");
        File.ReadAllText(Path.Combine(destinationDirectory.Path, "root.txt")).ShouldBe("root");
        File.ReadAllText(Path.Combine(destinationDirectory.Path, "nested", "child.txt")).ShouldBe("child");
        File.ReadAllText(Path.Combine(destinationDirectory.Path, ".mirrored-archive")).ShouldBe("ok");

        // When
        FileUtils.MirrorArchive(archive, destination);
        var secondPassFiles = ReadRelativeFileSnapshot(destinationDirectory.Path);

        // Then
        firstPassFiles.ShouldBe(secondPassFiles);
        File.ReadAllText(Path.Combine(destinationDirectory.Path, "root.txt")).ShouldBe("root");
        File.ReadAllText(Path.Combine(destinationDirectory.Path, "nested", "child.txt")).ShouldBe("child");
        File.ReadAllText(Path.Combine(destinationDirectory.Path, ".mirrored-archive")).ShouldBe("ok");
    }

    private static IReadOnlyList<string> ReadRelativeFileSnapshot(string directoryPath)
    {
        return Directory
            .EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(directoryPath, path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static FileInfo GetAssetFile(string fileName)
    {
        var file = new FileInfo(Path.Combine(AssetsDirectory.FullName, fileName));
        file.Exists.ShouldBeTrue($"Archive asset was not copied to the test output: {file.FullName}");
        return file;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), nameof(ArchiveMirrorFixture), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch
            {
            }
        }
    }
}
