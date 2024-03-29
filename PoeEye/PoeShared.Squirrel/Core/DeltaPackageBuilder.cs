using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DeltaCompressionDotNet.MsDelta;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Squirrel.Scaffolding;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Readers;
using Splat;
using Squirrel;
using ChecksumFailedException = PoeShared.Squirrel.Scaffolding.ChecksumFailedException;
using ReleasePackage = PoeShared.Squirrel.Core.ReleasePackage;

namespace PoeShared.Squirrel.Core;

public class DeltaPackageBuilder : IEnableLogger, IDeltaPackageBuilder
{
    private static readonly IFluentLog Log = typeof(DeltaPackageBuilder).PrepareLogger();

    private readonly string localAppDirectory;

    public DeltaPackageBuilder(string localAppDataOverride = null)
    {
        localAppDirectory = localAppDataOverride;
    }

    public ReleasePackage ApplyDeltaPackage(ReleasePackage basePackage, ReleasePackage deltaPackage, string outputFile)
    {
        Guard.ArgumentIsTrue(deltaPackage != null, "deltaPackage != null");
        Guard.ArgumentIsTrue(!string.IsNullOrEmpty(outputFile), "!string.IsNullOrEmpty(outputFile)");
        Guard.ArgumentIsTrue(!File.Exists(outputFile), "!File.Exists(outputFile)");

        Log.Debug($"Applying delta-package {deltaPackage} to base {basePackage}, local app directory: {localAppDirectory}");
        using (Utility.WithTempDirectory(out var deltaPath, localAppDirectory))
        using (Utility.WithTempDirectory(out var workingPath, localAppDirectory))
        {
            var opts = new ExtractionOptions {ExtractFullPath = true, Overwrite = true, PreserveFileTime = true};

            using (var za = ZipArchive.Open(deltaPackage.InputPackageFile))
            using (var reader = za.ExtractAllEntries())
            {
                reader.WriteAllToDirectory(deltaPath, opts);
            }

            using (var za = ZipArchive.Open(basePackage.InputPackageFile))
            using (var reader = za.ExtractAllEntries())
            {
                reader.WriteAllToDirectory(workingPath, opts);
            }

            var pathsVisited = new List<string>();

            var deltaPathRelativePaths = new DirectoryInfo(deltaPath).GetAllFilesRecursively()
                .Select(x => x.FullName.Replace(deltaPath + Path.DirectorySeparatorChar, ""))
                .ToArray();

            // Apply all of the .diff files
            deltaPathRelativePaths
                .Where(x => x.StartsWith("lib", StringComparison.InvariantCultureIgnoreCase))
                .Where(x => !x.EndsWith(".shasum", StringComparison.InvariantCultureIgnoreCase))
                .Where(
                    x => !x.EndsWith(".diff", StringComparison.InvariantCultureIgnoreCase) ||
                         !deltaPathRelativePaths.Contains(x.Replace(".diff", ".bsdiff")))
                .ForEach(
                    file =>
                    {
                        pathsVisited.Add(Regex.Replace(file, @"\.(bs)?diff$", "").ToLowerInvariant());
                        ApplyDiffToFile(deltaPath, file, workingPath);
                    });

            // Delete all of the files that were in the old package but
            // not in the new one.
            new DirectoryInfo(workingPath).GetAllFilesRecursively()
                .Select(x => x.FullName.Replace(workingPath + Path.DirectorySeparatorChar, "").ToLowerInvariant())
                .Where(x => x.StartsWith("lib", StringComparison.InvariantCultureIgnoreCase) && !pathsVisited.Contains(x))
                .ForEach(
                    x =>
                    {
                        Log.Info($"{x} was in old package but not in new one, deleting");
                        File.Delete(Path.Combine(workingPath, x));
                    });

            // Update all the files that aren't in 'lib' with the delta
            // package's versions (i.e. the nuspec file, etc etc).
            deltaPathRelativePaths
                .Where(x => !x.StartsWith("lib", StringComparison.InvariantCultureIgnoreCase))
                .ForEach(
                    x =>
                    {
                        Log.Info($"Updating metadata file: {x}");
                        File.Copy(Path.Combine(deltaPath, x), Path.Combine(workingPath, x), true);
                    });

            Log.Info($"Repacking into full package: {outputFile}");
            using (var za = ZipArchive.Create())
            using (var tgt = File.OpenWrite(outputFile ?? throw new ArgumentNullException(nameof(outputFile))))
            {
                za.DeflateCompressionLevel = CompressionLevel.BestSpeed;
                za.AddAllFromDirectory(workingPath);
                za.SaveTo(tgt);
            }
        }

        return new ReleasePackage(outputFile);
    }

    private void CreateDeltaForSingleFile(FileInfo targetFile, DirectoryInfo workingDirectory, Dictionary<string, string> baseFileListing)
    {
        // NB: There are three cases here that we'll handle:
        //
        // 1. Exists only in new => leave it alone, we'll use it directly.
        // 2. Exists in both old and new => write a dummy file so we know
        //    to keep it.
        // 3. Exists in old but changed in new => create a delta file
        //
        // The fourth case of "Exists only in old => delete it in new"
        // is handled when we apply the delta package
        var relativePath = targetFile.FullName.Replace(workingDirectory.FullName, "");

        if (!baseFileListing.ContainsKey(relativePath))
        {
            Log.Info($"{relativePath} not found in base package, marking as new");
            return;
        }

        var oldData = File.ReadAllBytes(baseFileListing[relativePath]);
        var newData = File.ReadAllBytes(targetFile.FullName);

        if (BytesAreIdentical(oldData, newData))
        {
            Log.Info($"{relativePath} hasn't changed, writing dummy file");

            File.Create(targetFile.FullName + ".diff").Dispose();
            File.Create(targetFile.FullName + ".shasum").Dispose();
            targetFile.Delete();
            return;
        }

        Log.Info($"Delta patching {baseFileListing[relativePath]} => {targetFile.FullName}");
        var msDelta = new MsDeltaCompression();

        if (targetFile.Extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
            targetFile.Extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
            targetFile.Extension.Equals(".node", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                msDelta.CreateDelta(baseFileListing[relativePath], targetFile.FullName, targetFile.FullName + ".diff");
                goto exit;
            }
            catch (Exception)
            {
                Log.Warn($"We couldn't create a delta for {targetFile.Name}, attempting to create bsdiff");
            }
        }

        try
        {
            using (var of = File.Create(targetFile.FullName + ".bsdiff"))
            {
                BinaryPatchUtility.Create(oldData, newData, of);

                // NB: Create a dummy corrupt .diff file so that older 
                // versions which don't understand bsdiff will fail out
                // until they get upgraded, instead of seeing the missing
                // file and just removing it.
                File.WriteAllText(targetFile.FullName + ".diff", "1");
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"We really couldn't create a delta for {targetFile.Name}", ex);

            Utility.DeleteFileHarder(targetFile.FullName + ".bsdiff", true);
            Utility.DeleteFileHarder(targetFile.FullName + ".diff", true);
            return;
        }

        exit:

        var rl = ReleaseEntry.GenerateFromFile(new MemoryStream(newData), targetFile.Name + ".shasum");
        File.WriteAllText(targetFile.FullName + ".shasum", rl.EntryAsString, Encoding.UTF8);
        targetFile.Delete();
    }


    private void ApplyDiffToFile(string deltaPath, string relativeFilePath, string workingDirectory)
    {
        var inputFile = Path.Combine(deltaPath, relativeFilePath);
        var finalTarget = Path.Combine(workingDirectory, Regex.Replace(relativeFilePath, @"\.(bs)?diff$", ""));

        Utility.WithTempFile(out var tempTargetFile, localAppDirectory);
        Log.Debug($"Applying diff to input file {inputFile}, target file: {finalTarget}, temp file: {tempTargetFile}");

        try
        {
            // NB: Zero-length diffs indicate the file hasn't actually changed
            if (new FileInfo(inputFile).Length == 0)
            {
                Log.Info($"{relativeFilePath} exists unchanged, skipping");
                return;
            }

            if (relativeFilePath.EndsWith(".bsdiff", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var of = File.OpenWrite(tempTargetFile))
                using (var inf = File.OpenRead(finalTarget))
                {
                    Log.Info($"Applying BSDiff to {relativeFilePath}");
                    BinaryPatchUtility.Apply(inf, () => File.OpenRead(inputFile), of);
                }

                VerifyPatchedFile(relativeFilePath, inputFile, tempTargetFile);
            }
            else if (relativeFilePath.EndsWith(".diff", StringComparison.InvariantCultureIgnoreCase))
            {
                Log.Info($"Applying MSDiff to {relativeFilePath}");
                var msDelta = new MsDeltaCompression();
                msDelta.ApplyDelta(inputFile, finalTarget, tempTargetFile);

                VerifyPatchedFile(relativeFilePath, inputFile, tempTargetFile);
            }
            else
            {
                using (var of = File.OpenWrite(tempTargetFile))
                using (var inf = File.OpenRead(inputFile))
                {
                    Log.Info($"Adding new file: {relativeFilePath}");
                    inf.CopyTo(of);
                }
            }

            if (File.Exists(finalTarget))
            {
                File.Delete(finalTarget);
            }

            var targetPath = Directory.GetParent(finalTarget);
            if (!targetPath.Exists)
            {
                targetPath.Create();
            }

            File.Move(tempTargetFile, finalTarget);
        }
        finally
        {
            if (File.Exists(tempTargetFile))
            {
                Utility.DeleteFileHarder(tempTargetFile, true);
            }
        }
    }

    private void VerifyPatchedFile(string relativeFilePath, string inputFile, string tempTargetFile)
    {
        var shaFile = Regex.Replace(inputFile, @"\.(bs)?diff$", ".shasum");
        var expectedReleaseEntry = ReleaseEntry.ParseReleaseEntry(File.ReadAllText(shaFile, Encoding.UTF8));
        var actualReleaseEntry = ReleaseEntry.GenerateFromFile(tempTargetFile);

        if (expectedReleaseEntry.Filesize != actualReleaseEntry.Filesize)
        {
            Log.Warn($"Patched file {relativeFilePath} has incorrect size, expected {expectedReleaseEntry.Filesize}, got {actualReleaseEntry.Filesize}");
            throw new ChecksumFailedException {Filename = relativeFilePath};
        }

        if (expectedReleaseEntry.SHA1 != actualReleaseEntry.SHA1)
        {
            Log.Warn(
                $"Patched file {relativeFilePath} has incorrect SHA1, expected {expectedReleaseEntry.SHA1}, got {actualReleaseEntry.SHA1}");
            throw new ChecksumFailedException {Filename = relativeFilePath};
        }
    }

    private bool BytesAreIdentical(byte[] oldData, byte[] newData)
    {
        if (oldData == null || newData == null)
        {
            return oldData == newData;
        }

        if (oldData.LongLength != newData.LongLength)
        {
            return false;
        }

        for (long i = 0; i < newData.LongLength; i++)
        {
            if (oldData[i] != newData[i])
            {
                return false;
            }
        }

        return true;
    }
}