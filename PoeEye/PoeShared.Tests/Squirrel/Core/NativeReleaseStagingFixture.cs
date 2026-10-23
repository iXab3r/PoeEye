using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using PoeShared.Squirrel.Core;
using Shouldly;
using Squirrel;

namespace PoeShared.Tests.Squirrel.Core;

[TestFixture]
[Category("Integration")]
public sealed class NativeReleaseStagingFixture
{
    /// <summary>
    /// WHAT: A downloaded native release can be staged for helper acknowledgement while the existing
    /// installation, RELEASES and version folders retain their bytes. This protects the found regression
    /// where preaccept preparation invoked full ApplyReleases and published installed state too early.
    /// HOW: Extract a real local full NUPKG into a unique staging tree and compare its payload plus every
    /// original installation file. This component test supplements the real native successor scenarios.
    /// </summary>
    [Test]
    public async Task ShouldStageDownloadedReleaseWithoutPublishingInstallation()
    {
        // Given
        var root = Path.Combine(Path.GetTempPath(), nameof(NativeReleaseStagingFixture), Guid.NewGuid().ToString("N"));
        var packages = Path.Combine(root, "packages");
        Directory.CreateDirectory(packages);
        try
        {
            Write(root, "packages/RELEASES", "original installed release inventory");
            Write(root, "app-1.0.0/EyeAuras.exe", "original running launcher");
            Write(root, "app-2.0.0/retained.txt", "existing version folder must survive staging");
            Write(root, "user-settings.json", "retained user data");
            var packagePath = Path.Combine(packages, "EyeAuras-2.0.0-full.nupkg");
            using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create))
            {
                foreach (var (name, value) in new[]
                {
                    ("lib/net45/EyeAuras.exe", "prepared launcher B"),
                    ("lib/net45/assets/nested.txt", "prepared companion B")
                })
                {
                    using var writer = new StreamWriter(archive.CreateEntry(name).Open(), Encoding.UTF8);
                    writer.Write(value);
                }
            }
            var expectedPayload = new System.Collections.Generic.Dictionary<string, byte[]>();
            using (var archive = ZipFile.OpenRead(packagePath))
            {
                foreach (var entry in archive.Entries)
                {
                    using var input = entry.Open();
                    using var bytes = new MemoryStream();
                    input.CopyTo(bytes);
                    expectedPayload.Add(entry.FullName["lib/net45/".Length..], bytes.ToArray());
                }
            }
            var release = ReleaseEntry.GenerateFromFile(packagePath);
            var update = PoeUpdateInfo.Create(null, Array.Empty<IReleaseEntry>(), new[] { release }, packages);
            var before = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .ToDictionary(path => path, File.ReadAllBytes);

            // When
            var staged = await new ApplyReleasesImpl(root).StageReleases(update);

            // Then
            Path.GetFullPath(staged).ShouldStartWith(Path.Combine(root, ".updater", "staging") + Path.DirectorySeparatorChar);
            foreach (var (relativePath, bytes) in expectedPayload)
                File.ReadAllBytes(Path.Combine(staged, relativePath)).ShouldBe(bytes, relativePath);
            Directory.EnumerateFiles(staged, "*", SearchOption.AllDirectories).Count().ShouldBe(2);
            foreach (var (path, bytes) in before) File.ReadAllBytes(path).ShouldBe(bytes, path);
            Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Where(path => !path.StartsWith(staged + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path).ShouldBe(before.Keys.OrderBy(path => path));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void Write(string root, string relativePath, string value)
    {
        var path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, value);
    }
}
