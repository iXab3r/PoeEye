#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Services;

internal sealed class SevenZipNativeWrapper : ISevenZipWrapper
{
    private static readonly IFluentLog Log = typeof(SevenZipNativeWrapper).PrepareLogger();
    private static readonly string SevenZipDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "x64", "7z.dll");

    private static readonly object Gate = new();
    private static Kernel32.SafeLibraryHandle? sevenZipDllHandle;

    static SevenZipNativeWrapper()
    {
        if (IntPtr.Size != 8)
        {
            throw new PlatformNotSupportedException("SevenZipNativeWrapper only supports x64 process.");
        }
    }

    public SevenZipNativeWrapper()
    {
        EnsureLibraryLoaded();
    }

    public void CreateArchive(FileInfo outputFileName, IReadOnlyList<FileInfo> filesToAdd)
    {
        EnsureLibraryLoaded();

        if (outputFileName == null)
        {
            throw new ArgumentNullException(nameof(outputFileName));
        }

        if (filesToAdd == null)
        {
            throw new ArgumentNullException(nameof(filesToAdd));
        }

        var filesDescription = filesToAdd
            .Select(x => x == null! ? "<null>" : $"{x.Name} ({x.Length}b)")
            .JoinStrings(", ");
        
        Log.Info($"Creating archive {outputFileName}, files: {filesDescription}");
        
        var uniqueFiles = filesToAdd
            .Where(x => x != null!)
            .Select(x => x.FullName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (uniqueFiles.Length != filesToAdd.Count)
        {
            var allFiles = filesToAdd.Select(x => x?.FullName).JoinStrings(", ");
            Log.Warn($"File list contains duplicates/nulls: {allFiles}");
        }

        if (outputFileName.Exists)
        {
            try
            {
                outputFileName.Delete();
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to delete existing archive {outputFileName.FullName}, will try to overwrite. Error: {e.Message}");
            }
        }

        var compressor = new SevenZip.SevenZipCompressor
        {
            ArchiveFormat = SevenZip.OutArchiveFormat.SevenZip,
            CompressionLevel = SevenZip.CompressionLevel.Normal
        };

        compressor.CompressFiles(outputFileName.FullName, uniqueFiles);

        outputFileName.Refresh();
        if (!outputFileName.Exists)
        {
            throw new FileNotFoundException($"Could not add/update archive {outputFileName} - file not found after operation");
        }

        Log.Info($"Created/updated archive {outputFileName}, size: {outputFileName.Length}b");
    }

    public void ExtractArchive(FileInfo inputFileName, DirectoryInfo outputDirectory)
    {
        ExtractArchive(new SevenZipExtractArguments(inputFileName, outputDirectory));
    }

    public void ExtractArchive(SevenZipExtractArguments arguments)
    {
        EnsureLibraryLoaded();

        if (arguments == null)
        {
            throw new ArgumentNullException(nameof(arguments));
        }

        Log.Info($"Extracting archive, args: {arguments}");
        if (!arguments.OutputDirectory.Exists)
        {
            Log.Info($"Creating output directory {arguments.OutputDirectory}");
            arguments.OutputDirectory.Create();
        }

        // Use SevenZipExtractor to extract. It overwrites existing files by default.
        using (var extractor = new SevenZip.SevenZipExtractor(arguments.Archive.FullName))
        {
            extractor.ExtractArchive(arguments.OutputDirectory.FullName);
        }

        Log.Info($"Output directory contains following files: {arguments.OutputDirectory.EnumerateFiles().Select(x => $"{x.Name} ({x.Length}b)").JoinStrings(", ")}");
    }

    public void CreateFromDirectory(DirectoryInfo sourceDirectory, FileInfo archivePath, System.IO.Compression.CompressionLevel compressionLevel = System.IO.Compression.CompressionLevel.NoCompression)
    {
        EnsureLibraryLoaded();

        Log.Info($"Creating archive from directory: {sourceDirectory.FullName} to {archivePath.FullName}");

        if (!sourceDirectory.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectory.FullName}");
        }

        if (archivePath.Exists)
        {
            try
            {
                archivePath.Delete();
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to delete existing archive {archivePath.FullName}, will try to overwrite. Error: {e.Message}");
            }
        }

        var targetDir = archivePath.Directory;
        if (targetDir == null)
        {
            throw new ArgumentException($"Archive path does not have a valid directory: {archivePath.FullName}", nameof(archivePath));
        }

        if (!targetDir.Exists)
        {
            targetDir.Create();
        }

        var compressor = new SevenZip.SevenZipCompressor
        {
            ArchiveFormat = SevenZip.OutArchiveFormat.SevenZip,
            CompressionLevel = MapCompressionLevel(compressionLevel)
        };

        // Recursively include subdirectories
        compressor.CompressDirectory(sourceDirectory.FullName, archivePath.FullName);

        archivePath.Refresh();
        if (!archivePath.Exists)
        {
            throw new FileNotFoundException($"Failed to create archive: {archivePath.FullName}");
        }

        Log.Info($"Archive created successfully at {archivePath.FullName}, size: {archivePath.Length} bytes");
    }

    private static SevenZip.CompressionLevel MapCompressionLevel(System.IO.Compression.CompressionLevel level)
    {
        return level switch
        {
            System.IO.Compression.CompressionLevel.NoCompression => SevenZip.CompressionLevel.None,
            System.IO.Compression.CompressionLevel.Fastest => SevenZip.CompressionLevel.Fast,
            System.IO.Compression.CompressionLevel.Optimal => SevenZip.CompressionLevel.Normal, // or High
            System.IO.Compression.CompressionLevel.SmallestSize => SevenZip.CompressionLevel.Ultra,
            _ => SevenZip.CompressionLevel.Normal
        };
    }

    private static void EnsureLibraryLoaded()
    {
        if (IsLibraryLoadedOrThrow())
        {
            return;
        }

        lock (Gate)
        {
            if (IsLibraryLoadedOrThrow())
            {
                return;
            }

            if (!File.Exists(SevenZipDllPath))
            {
                var msg = $"7z.dll not found at {SevenZipDllPath}";
                Log.Error(msg);
                throw new FileNotFoundException(msg, SevenZipDllPath);
            }

            sevenZipDllHandle = Kernel32.LoadLibrary(SevenZipDllPath);
            if (sevenZipDllHandle == null || sevenZipDllHandle.IsInvalid)
            {
                var error = Kernel32.GetLastError();
                var msg = $"LoadLibrary failed for {SevenZipDllPath}, Win32 error: {error}";
                Log.Error(msg);
                throw new Win32Exception(error, msg);
            }

            SevenZip.SevenZipBase.SetLibraryPath(SevenZipDllPath);
            Log.Info($"SevenZip native library loaded from {SevenZipDllPath}, handle: {sevenZipDllHandle.DangerousGetHandle().ToHexadecimal()}");
        }

        return;

        static bool IsLibraryLoadedOrThrow()
        {
            if (sevenZipDllHandle == null)
            {
                return false;
            }

            if (sevenZipDllHandle.IsInvalid)
            {
                throw new InvalidOperationException("Could not load 7z.dll, handle is invalid");
            }

            return true;
        }
    }
}