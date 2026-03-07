using System;
using System.IO;
using System.Linq;
using DynamicData;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using PoeShared.IO;
using PoeShared.Scaffolding;

namespace PoeShared.Tests.Scaffolding;

public abstract class FileProviderFixtureBase : FixtureBase
{
    private static readonly DirectoryInfo BaseFixtureDirectory = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scaffolding", nameof(FileProviderFixtureBase)));

    protected DirectoryInfo TempDirectory { get; private set; }

    protected PhysicalFileProvider PhysicalFileProvider { get; private set; }

    protected InMemoryFileProvider InMemoryFileProvider { get; private set; }

    protected override void SetUp()
    {
        BaseFixtureDirectory.Create();
        TempDirectory = new DirectoryInfo(Path.Combine(BaseFixtureDirectory.FullName, $"{GetType().Name}_{Guid.NewGuid():N}"));
        TempDirectory.Create();

        SeedFileSystem();

        PhysicalFileProvider = new PhysicalFileProvider(TempDirectory.FullName);
        InMemoryFileProvider = CreateInMemoryFileProvider();
    }

    protected override void TearDown()
    {
        PhysicalFileProvider?.Dispose();

        if (TempDirectory?.Exists == true)
        {
            TempDirectory.Delete(true);
        }
    }

    protected virtual void SeedFileSystem()
    {
        WriteFile("root.txt", "root");
        WriteFile("a+b.txt", "plus");
        WriteFile("assets/site.css", "site");
        WriteFile("assets/app.js", "app");
        WriteFile("assets/nested/deep.txt", "deep");
        WriteFile("assets/nested/LICENSE", "license");
        WriteFile("docs/readme", "readme");
        WriteFile("docs/readme.txt", "readme.txt");
        WriteFile("docs/archive.tar.gz", "archive");
        WriteFile("encoded/plus+space.txt", "plus+space");
        WriteFile("encoded/space name.txt", "space name");
        WriteFile("scripts/app.js", "script");
    }

    protected string RelativePath(FileSystemInfo fileSystemInfo)
    {
        return new OSPath(Path.GetRelativePath(TempDirectory.FullName, fileSystemInfo.FullName)).AsUnixPath;
    }

    protected string[] GetExpectedFilePaths(string searchPattern, EnumerationOptions enumerationOptions)
    {
        return TempDirectory
            .GetFiles(searchPattern, enumerationOptions)
            .Select(RelativePath)
            .OrderBy(x => x, PathUtils.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .ToArray();
    }

    protected string[] GetExpectedDirectoryPaths(string searchPattern, EnumerationOptions enumerationOptions)
    {
        return TempDirectory
            .GetDirectories(searchPattern, enumerationOptions)
            .Select(RelativePath)
            .OrderBy(x => x, PathUtils.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .ToArray();
    }

    protected void WriteFile(string relativePath, string content)
    {
        var filePath = Path.Combine(TempDirectory.FullName, relativePath);
        var directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(filePath, content);
    }

    private InMemoryFileProvider CreateInMemoryFileProvider()
    {
        var fileProvider = new InMemoryFileProvider();
        foreach (var fileInfo in TempDirectory.GetFiles("*", SearchOption.AllDirectories))
        {
            fileProvider.FilesByName.AddOrUpdate(new InMemoryFileInfo(RelativePath(fileInfo), File.ReadAllBytes(fileInfo.FullName), fileInfo.LastWriteTimeUtc));
        }

        return fileProvider;
    }
}
