using System;
using System.IO;

namespace PoeShared.Services;

public sealed record SevenZipExtractArguments(FileInfo Archive, DirectoryInfo OutputDirectory)
{
    public FileInfo Archive { get; } = Archive ?? throw new ArgumentNullException(nameof(Archive));

    public DirectoryInfo OutputDirectory { get; } = OutputDirectory ?? throw new ArgumentNullException(nameof(OutputDirectory));

    public bool OverwriteAll { get; set; } = true;
}