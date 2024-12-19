using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Services;

internal sealed class SevenZipWrapper : ISevenZipWrapper
{
    private static readonly IFluentLog Log = typeof(SevenZipWrapper).PrepareLogger();
    private static readonly string SevenZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7za.exe");

    public SevenZipWrapper()
    {
            
    }

    public void AddToArchive(FileInfo outputFileName, IReadOnlyList<FileInfo> filesToAdd)
    {
        Log.Info($"Adding to archive {outputFileName} files: {filesToAdd.Select(x => $"{x.Name} ({x.Length}b)").JoinStrings(", ")}");

        var uniqueFiles = filesToAdd.Select(x => x.FullName).ToHashSet();
        if (uniqueFiles.Count != filesToAdd.Count)
        {
            Log.Warn($"File list contains duplicates: {filesToAdd.Select(x => x.FullName)}");
        }
        var processStartInfo = PrepareProcessStartInfo();
        var args = new List<string>
        {
            "a",
            "-mx=3", // fast
            $"\"{outputFileName.FullName}\"",
        };
        uniqueFiles.Select(x => $"\"{x}\"").ForEach(args.Add);
        processStartInfo.Arguments = args.JoinStrings(" ");
        ProcessHelper.RunCmd(processStartInfo);
            
        outputFileName.Refresh();
        if (!outputFileName.Exists)
        {
            throw new FileNotFoundException($"Could not add/update archive {outputFileName} - file not found after operation");
        }
            
        Log.Info($"Created/updated archive {outputFileName}, size: {outputFileName.Length}b");
    }

    public void ExtractArchive(
        FileInfo inputFileName, 
        DirectoryInfo outputDirectory)
    {
        ExtractArchive(new SevenZipExtractArguments(inputFileName, outputDirectory));
    }

    public void ExtractArchive(SevenZipExtractArguments arguments)
    {
        Log.Info($"Extracting archive, args: {arguments}");
        if (!arguments.OutputDirectory.Exists)
        {
            Log.Info($"Creating output directory {arguments.OutputDirectory}");
            arguments.OutputDirectory.Create();
        }

        var processStartInfo = PrepareProcessStartInfo();
        var args = new List<string>
        {
            "x", // extract with full paths
            $"\"{arguments.Archive.FullName}\"",
            $"\"-o{arguments.OutputDirectory.FullName}\"",
            arguments.OverwriteAll ? "-aoa" : string.Empty
        };
        processStartInfo.Arguments = args.JoinStrings(" ");
        ProcessHelper.RunCmd(processStartInfo);
        Log.Info($"Output directory contains following files: {arguments.OutputDirectory.EnumerateFiles().Select(x => $"{x.Name} ({x.Length}b)").JoinStrings(", ")}");
    }
    
    public void CreateFromDirectory(
        DirectoryInfo sourceDirectory,
        FileInfo archivePath,
        CompressionLevel compressionLevel = CompressionLevel.NoCompression)
    {
        Log.Info($"Creating archive from directory: {sourceDirectory.FullName} to {archivePath.FullName}");

        if (!sourceDirectory.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectory.FullName}");
        }

        var processStartInfo = PrepareProcessStartInfo();

        // Set compression level based on the input
        var compressionArg = compressionLevel switch
        {
            CompressionLevel.Fastest => "-mx=1",
            CompressionLevel.Optimal => "-mx=9",
            CompressionLevel.NoCompression => "-mx=0",
            _ => "-mx=5"
        };

        var args = new List<string>
        {
            "a", // Add files to archive
            compressionArg,
            $"\"{archivePath.FullName}\"",
            $"\"{Path.Combine(sourceDirectory.FullName, "*")}\"", // Add all files
            "-r" // Recursively include subdirectories
        };

        processStartInfo.Arguments = args.JoinStrings(" ");

        ProcessHelper.RunCmd(processStartInfo);

        archivePath.Refresh();
        if (!archivePath.Exists)
        {
            throw new FileNotFoundException($"Failed to create archive: {archivePath.FullName}");
        }

        Log.Info($"Archive created successfully at {archivePath.FullName}, size: {archivePath.Length} bytes");
    }

    private static ProcessStartInfo PrepareProcessStartInfo()
    {
        return new ProcessStartInfo(SevenZipPath)
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    }
}