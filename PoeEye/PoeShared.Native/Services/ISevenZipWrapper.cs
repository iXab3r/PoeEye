using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace PoeShared.Services;

public interface ISevenZipWrapper
{
    void CreateArchive(FileInfo outputFileName, IReadOnlyList<FileInfo> filesToAdd);
    void ExtractArchive(FileInfo inputFileName, DirectoryInfo outputDirectory);
    void ExtractArchive(SevenZipExtractArguments arguments);

    void CreateFromDirectory(
        DirectoryInfo sourceDirectory,
        FileInfo archivePath,
        CompressionLevel compressionLevel = CompressionLevel.NoCompression);
}