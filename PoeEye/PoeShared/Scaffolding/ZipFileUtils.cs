using System.IO.Compression;
using ByteSizeLib;
using PoeShared.IO;

namespace PoeShared.Scaffolding;

public static class ZipFileUtils
{
    public static FileInfo CreateFromDirectory(
        DirectoryInfo sourceDirectory, 
        OSPath archivePath,
        CompressionLevel compressionLevel = CompressionLevel.NoCompression,
        IProgressReporter progressReporter = default)
    {
        var files = sourceDirectory.GetFilesSafe("*", SearchOption.AllDirectories);
        var totalProcessedFiles = 0;
        using var zipToOpen = new FileStream(archivePath.FullName, FileMode.Create);
        using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create);
        foreach (var file in files)
        {
            var entryName = Path.GetRelativePath(sourceDirectory.FullName, file.FullName);
            var entry = archive.CreateEntry(entryName, compressionLevel);

            using (var entryStream = entry.Open())
            using (var fileStream = file.OpenRead())
            {
                fileStream.CopyTo(entryStream);
            }

            totalProcessedFiles++;
            
            progressReporter?.Update(current: totalProcessedFiles, files.Length);
        }

        var resultArchive = new FileInfo(archivePath.FullName);
        if (!resultArchive.Exists)
        {
            throw new ArgumentException($"Failed to pack directory {sourceDirectory} to {archivePath} - archive not created");
        }

        return resultArchive;
    }
}