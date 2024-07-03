using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PoeShared.Scaffolding;
using PoeShared.Logging;
using PoeShared.Squirrel.Scaffolding;
using ReactiveUI;
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
        PackagesPath = Path.Combine(this.rootAppDirectory, "packages");
    }
    
    public string PackagesPath { get; }

    public async Task<bool> VerifyReleases(
        IReadOnlyCollection<IReleaseEntry> releasesToDownload,
        Action<int> progress = null)
    {
        progress ??= _ => { };
        using var progressTracker = new ComplexProgressTracker();
        using var progressUpdater = progressTracker.WhenAnyValue(x => x.ProgressPercent).Subscribe(x => progress(x));

        return await releasesToDownload.ToAsyncEnumerable()
            .AllAsync(x =>
            {
                try
                {
                    return ValidateChecksum(x);
                }
                finally
                {
                    lock (progressTracker)
                    {
                        progressTracker.Update(100, x.Filename);
                    }
                }
            });;
    }

    public async Task<IReadOnlyCollection<FileInfo>> DownloadReleases(
        string updateUrlOrPath,
        IReadOnlyCollection<IReleaseEntry> releasesToDownload,
        Action<int> progress = null)
    {
        progress ??= (_ => { });

        Directory.CreateDirectory(PackagesPath);

        double current = 0;
        var toIncrement = 100.0 / releasesToDownload.Count;

        var downloadedFiles = new List<FileInfo>();
        if (Utility.IsHttpUrl(updateUrlOrPath))
        {
            // From Internet
            await releasesToDownload.ForEachAsync(
                async x =>
                {
                    var targetFile = new FileInfo(Path.Combine(PackagesPath, x.Filename));
                    
                    if (!ValidateChecksum(x))
                    {
                        Log.Debug($"Downloading from {updateUrlOrPath} to {targetFile.FullName}");
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
                    }
                    else
                    {
                        lock (progress)
                        {
                            progress((int) Math.Round(current += toIncrement));
                        }
                    }

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
                    var targetFile = new FileInfo(Path.Combine(PackagesPath, x.Filename));

                    if (!ValidateChecksum(x))
                    {
                        File.Copy(
                            Path.Combine(updateUrlOrPath, x.Filename),
                            targetFile.FullName,
                            true);
                    } 

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

    private string CalculateChecksum(FileInfo targetPackage)
    {
        using var file = targetPackage.OpenRead();
        var hash = Utility.CalculateStreamSha1(file);
        return hash;
    }

    private bool ValidateChecksum(IReleaseEntry downloadedRelease)
    {
        try
        {
            ChecksumPackage(downloadedRelease);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    private void ChecksumPackage(IReleaseEntry downloadedRelease)
    {
        var targetPackage = new FileInfo(Path.Combine(rootAppDirectory, "packages", downloadedRelease.Filename));

        if (!targetPackage.Exists)
        {
            Log.Debug($"File {targetPackage.FullName} should exist but doesn't");

            throw new Exception("Checksum file doesn't exist: " + targetPackage.FullName);
        }

        if (targetPackage.Length != downloadedRelease.Filesize)
        {
            Log.Debug($"File Length should be {downloadedRelease.Filesize}, is {targetPackage.Length}");
            targetPackage.Delete();

            throw new Exception("Checksum file size doesn't match: " + targetPackage.FullName);
        }

        var hash = CalculateChecksum(targetPackage);
        if (hash.Equals(downloadedRelease.SHA1, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Log.Debug($"File SHA1 should be {downloadedRelease.SHA1}, is {hash}");
        targetPackage.Delete();
        throw new Exception("Checksum doesn't match: " + targetPackage.FullName);
    }
}