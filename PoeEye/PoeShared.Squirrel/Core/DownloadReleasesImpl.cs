using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PoeShared.Scaffolding;
using PoeShared.Logging;
using PoeShared.Squirrel.Scaffolding;
using Splat;
using Squirrel;

namespace PoeShared.Squirrel.Core;

internal class DownloadReleasesImpl : IEnableLogger
{
    private static readonly IFluentLog Log = typeof(DownloadReleasesImpl).PrepareLogger();

    private readonly IFileDownloader urlDownloader;
    private readonly string rootAppDirectory;

    public DownloadReleasesImpl(IFileDownloader urlDownloader, string rootAppDirectory)
    {
        this.urlDownloader = urlDownloader;
        this.rootAppDirectory = rootAppDirectory;
    }

    public async Task<IReadOnlyCollection<FileInfo>> DownloadReleases(
        string updateUrlOrPath,
        IReadOnlyCollection<IReleaseEntry> releasesToDownload,
        Action<int> progress = null)
    {
        progress ??= (_ => { });

        var packagesDirectory = Path.Combine(rootAppDirectory, "packages");

        double current = 0;
        var toIncrement = 100.0 / releasesToDownload.Count;

        var downloadedFiles = new List<FileInfo>();
        if (Utility.IsHttpUrl(updateUrlOrPath))
        {
            // From Internet
            await releasesToDownload.ForEachAsync(
                async x =>
                {
                    var targetFile = new FileInfo(Path.Combine(packagesDirectory, x.Filename));

                    double component = 0;
                    await DownloadRelease(
                        updateUrlOrPath,
                        x,
                        urlDownloader,
                        targetFile.FullName,
                        p =>
                        {
                            lock (progress)
                            {
                                current -= component;
                                component = toIncrement / 100.0 * p;
                                progress((int) Math.Round(current += component));
                            }
                        });
                    ChecksumPackage(x);

                    targetFile.AddTo(downloadedFiles);
                });
        }
        else
        {
            // From Disk
            await releasesToDownload.ForEachAsync(
                x =>
                {
                    var targetFile = new FileInfo(Path.Combine(packagesDirectory, x.Filename));

                    File.Copy(
                        Path.Combine(updateUrlOrPath, x.Filename),
                        targetFile.FullName,
                        true);

                    lock (progress)
                    {
                        progress((int) Math.Round(current += toIncrement));
                    }

                    ChecksumPackage(x);

                    targetFile.AddTo(downloadedFiles);
                });
        }

        return downloadedFiles;
    }

    private static async Task DownloadRelease(
        string updateBaseUrl,
        IReleaseEntry releaseEntry,
        IFileDownloader urlDownloader, string targetFile,
        Action<int> progressConsumer)
    {
        var baseUri = Utility.EnsureTrailingSlash(new Uri(updateBaseUrl));

        if (!(releaseEntry is ReleaseEntry rawReleaseEntry))
        {
            throw new ApplicationException($"Release entry: {releaseEntry} is not downloadable");
        }

        var releaseEntryUrl = rawReleaseEntry.BaseUrl + rawReleaseEntry.Filename + rawReleaseEntry.Query;
        var sourceFileUrl = new Uri(baseUri, releaseEntryUrl).AbsoluteUri;
        File.Delete(targetFile);

        try
        {
            await urlDownloader.DownloadFile(sourceFileUrl, targetFile, progressConsumer);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to download from {updateBaseUrl}", e);
            progressConsumer(0);
        }
    }

    private void ChecksumPackage(IReleaseEntry downloadedRelease)
    {
        var targetPackage = new FileInfo(
            Path.Combine(rootAppDirectory, "packages", downloadedRelease.Filename));

        if (!targetPackage.Exists)
        {
            Log.Error($"File {targetPackage.FullName} should exist but doesn't");

            throw new Exception("Checksum file doesn't exist: " + targetPackage.FullName);
        }

        if (targetPackage.Length != downloadedRelease.Filesize)
        {
            Log.Error($"File Length should be {downloadedRelease.Filesize}, is {targetPackage.Length}");
            targetPackage.Delete();

            throw new Exception("Checksum file size doesn't match: " + targetPackage.FullName);
        }

        using var file = targetPackage.OpenRead();
        var hash = Utility.CalculateStreamSha1(file);
        if (hash.Equals(downloadedRelease.SHA1, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Log.Error($"File SHA1 should be {downloadedRelease.SHA1}, is {hash}");
        targetPackage.Delete();
        throw new Exception("Checksum doesn't match: " + targetPackage.FullName);
    }
}