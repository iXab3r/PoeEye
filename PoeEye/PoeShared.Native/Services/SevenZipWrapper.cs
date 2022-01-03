using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Services
{
    internal sealed class SevenZipWrapper : ISevenZipWrapper
    {
        private static readonly IFluentLog Log = typeof(SevenZipWrapper).PrepareLogger();
        private static readonly string SevenZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7za.exe");

        public SevenZipWrapper()
        {
            
        }

        public void AddToArchive(FileInfo outputFileName, IReadOnlyList<FileInfo> filesToAdd)
        {
            Log.Debug(() => $"Adding to archive {outputFileName} files: {filesToAdd.Select(x => $"{x.Name} ({x.Length}b)").JoinStrings(", ")}");

            var uniqueFiles = filesToAdd.ToHashSet();
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
            
            Log.Debug(() => $"Created/updated archive {outputFileName}, size: {outputFileName.Length}b");
        }

        public void ExtractArchive(FileInfo inputFileName, DirectoryInfo outputDirectory)
        {
            Log.Debug(() => $"Extracting archive {inputFileName} to {outputDirectory}");
            if (!outputDirectory.Exists)
            {
                Log.Debug(() => $"Creating output directory {outputDirectory}");
                outputDirectory.Create();
            }

            var processStartInfo = PrepareProcessStartInfo();
            var args = new List<string>
            {
                "e",
                $"\"{inputFileName.FullName}\"",
                $"\"-o{outputDirectory.FullName}\"",
            };
            processStartInfo.Arguments = args.JoinStrings(" ");
            ProcessHelper.RunCmd(processStartInfo);
            
            Log.Debug(() => $"Output directory contains following files: {outputDirectory.EnumerateFiles().Select(x => $"{x.Name} ({x.Length}b)").JoinStrings(", ")}");
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
}