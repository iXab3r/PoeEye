using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NuGet;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using SharpCompress.Archives.Zip;
using SharpCompress.Readers;
using Splat;
using Squirrel;

namespace PoeShared.Squirrel.Core;

public class ReleasePackage : IEnableLogger, IReleasePackage
{
    private static readonly IFluentLog Log = typeof(ReleasePackage).PrepareLogger();

    public ReleasePackage(string inputPackageFile, bool isReleasePackage = false)
    {
        InputPackageFile = inputPackageFile;

        if (isReleasePackage)
        {
            ReleasePackageFile = inputPackageFile;
        }
    }

    public SemanticVersion Version => InputPackageFile.ToSemanticVersion();

    public string InputPackageFile { get; }
        
    public string ReleasePackageFile { get; }

    public override string ToString()
    {
        return new { InputPackageFile, ReleasePackageFile, Version }.ToString();
    }

    public static Task ExtractZipForInstall(string zipFilePath, string outFolder, string rootPackageFolder)
    {
        var re = new Regex(@"lib[\\\/][^\\\/]*[\\\/]", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        return Task.Run(
            () =>
            {
                using (var za = ZipArchive.Open(zipFilePath))
                using (var reader = za.ExtractAllEntries())
                {
                    while (reader.MoveToNextEntry())
                    {
                        var parts = reader.Entry.Key.Split('\\', '/');
                        var decoded = string.Join(Path.DirectorySeparatorChar.ToString(), parts);

                        if (!re.IsMatch(decoded))
                        {
                            continue;
                        }

                        decoded = re.Replace(decoded, "", 1);

                        var fullTargetFile = Path.Combine(outFolder, decoded);
                        var fullTargetDir = Path.GetDirectoryName(fullTargetFile);
                        Directory.CreateDirectory(fullTargetDir);

                        var failureIsOkay = false;
                        if (!reader.Entry.IsDirectory && decoded.Contains("_ExecutionStub.exe"))
                        {
                            // NB: On upgrade, many of these stubs will be in-use, nbd tho.
                            failureIsOkay = true;

                            fullTargetFile = Path.Combine(
                                rootPackageFolder,
                                Path.GetFileName(decoded).Replace("_ExecutionStub.exe", ".exe"));

                            LogHost.Default.Info($"Rigging execution stub for {decoded} to {fullTargetFile}");
                        }

                        try
                        {
                            Scaffolding.Utility.Retry(
                                () =>
                                {
                                    if (reader.Entry.IsDirectory)
                                    {
                                        Directory.CreateDirectory(fullTargetFile);
                                    }
                                    else
                                    {
                                        reader.WriteEntryToFile(fullTargetFile);
                                    }
                                },
                                5);
                        }
                        catch (Exception e)
                        {
                            if (!failureIsOkay)
                            {
                                throw;
                            }

                            Log.Warn("Can't write execution stub, probably in use", e);
                        }
                    }
                }
            });
    }
}