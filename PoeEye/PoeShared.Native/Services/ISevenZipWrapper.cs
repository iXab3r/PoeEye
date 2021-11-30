using System.Collections.Generic;
using System.IO;

namespace PoeShared.Services
{
    public interface ISevenZipWrapper
    {
        void AddToArchive(FileInfo outputFileName, IReadOnlyList<FileInfo> filesToAdd);
        void ExtractArchive(FileInfo inputFileName, DirectoryInfo outputDirectory);
    }
}