using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PoeShared.Squirrel.Scaffolding;
using Splat;
using Squirrel;
using IFileDownloader = PoeShared.Services.IFileDownloader;

namespace PoeShared.Squirrel.Core;

public sealed class PortableUpdateManager : DisposableReactiveObject, IPoeUpdateManager
{
    private static readonly IFluentLog Log = typeof(PortableUpdateManager).PrepareLogger();

    private readonly IFileDownloader urlDownloader;
    
    public PortableUpdateManager(
        string urlOrPath,
        IFileDownloader urlDownloader,
        DirectoryInfo rootDirectory)
    {
        Guard.ArgumentIsTrue(!string.IsNullOrEmpty(urlOrPath), "!string.IsNullOrEmpty(urlOrPath)");

        UpdateUrlOrPath = urlOrPath;
        this.urlDownloader = urlDownloader;
        RootAppDirectory = rootDirectory.FullName;
    }
    
    public string UpdateUrlOrPath { get; }

    public string RootAppDirectory { get; }
    
    public async Task<IPoeUpdateInfo> CheckForUpdate(bool ignoreDeltaUpdates, Action<int> progress = null)
    {
        using var sw = new BenchmarkTimer($"Checking for updates, ignore delta: {ignoreDeltaUpdates}", Log);
        if (!ignoreDeltaUpdates)
        {
            throw new NotSupportedException("Delta-updates are not supported");
        }
        using var updateLock = AcquireUpdateLock();
            
        sw.Step("Update lock acquired");
        var checkForUpdate = new CheckForUpdateImpl(urlDownloader, RootAppDirectory);
        var localReleases = Array.Empty<ReleaseEntry>();
        var result = await checkForUpdate.CheckForUpdate(
            localReleases,
            UpdateUrlOrPath,
            ignoreDeltaUpdates: true,
            progress);
        sw.Step("Update check completed");
        return result;
    }

    public async Task<IPoeUpdateInfo> CheckForUpdate(Version targetVersion, Action<int> progress = null)
    {
        using var sw = new BenchmarkTimer($"Checking for updates, target version: {targetVersion}", Log);
        using var updateLock = AcquireUpdateLock();
            
        sw.Step("Update lock acquired");
        var checkForUpdate = new CheckForUpdateImpl(urlDownloader, RootAppDirectory);
        var result = await checkForUpdate.CheckForUpdate(
            UpdateUrlOrPath,
            targetVersion,
            progress);
        sw.Step("Update check completed");
        return result;
    }

    public async Task<IReadOnlyCollection<FileInfo>> DownloadReleases(IReadOnlyCollection<IReleaseEntry> releasesToDownload, Action<int> progress = null)
    {
        using var sw = new BenchmarkTimer($"Download releases: {releasesToDownload.Select(x => new { x.Version, x.IsDelta, x.Filesize }).DumpToString()}", Log);
        using var updateLock = AcquireUpdateLock();
        sw.Step("Update lock acquired");

        sw.Step("Downloading releases");
        var downloadReleases = new DownloadReleasesImpl(urlDownloader, RootAppDirectory);
        var result = await downloadReleases.DownloadReleases(UpdateUrlOrPath, releasesToDownload, progress);
        sw.Step("Download completed");
        return result;
    }

    public async Task<bool> VerifyReleases(IReadOnlyCollection<IReleaseEntry> releasesToDownload, Action<int> progress = null)
    {
        using var sw = new BenchmarkTimer($"Download releases: {releasesToDownload.Select(x => new { x.Version, x.IsDelta, x.Filesize }).DumpToString()}", Log);
        using var updateLock = AcquireUpdateLock();
        sw.Step("Update lock acquired");
        
        sw.Step("Verifying releases");
        var downloadReleases = new DownloadReleasesImpl(urlDownloader, RootAppDirectory);
        var result = await downloadReleases.VerifyReleases(releasesToDownload, progress);
        sw.Step($"Verified releases, result: {result}");
        return result;
    }

    public async Task<string> ApplyReleases(IPoeUpdateInfo updateInfo, Action<int> progress = null)
    {
        using var sw = new BenchmarkTimer($"Apply releases: {updateInfo.ReleasesToApply.Select(x => new { x.Version, x.IsDelta, x.Filename }).DumpToString()}", Log);
        using var updateLock = AcquireUpdateLock();
            
        sw.Step("Update lock acquired");
        var applyReleases = new ApplyReleasesImpl(RootAppDirectory);
        var result = await applyReleases.ApplyReleases(updateInfo, false, false, progress);
        sw.Step("All releases successfully applied");
        return result;
    }

    public async Task<IPoeUpdateInfo> PrepareUpdate(bool ignoreDeltaUpdates, IReadOnlyCollection<IReleaseEntry> localReleases, IReadOnlyCollection<IReleaseEntry> remoteReleases)
    {
        if (!ignoreDeltaUpdates)
        {
            throw new NotSupportedException("Delta-updates are not supported");
        }
        
        var checkForUpdate = new CheckForUpdateImpl(urlDownloader, RootAppDirectory);
        return checkForUpdate.DetermineUpdateInfo(localReleases, remoteReleases, ignoreDeltaUpdates: true);
    }

    private IDisposable AcquireUpdateLock()
    {
        return AcquireUpdateLock(RootAppDirectory);
    }
        
    private static IDisposable AcquireUpdateLock(string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        using var keyStream = new MemoryStream(keyBytes);
        var keyHash = Utility.CalculateStreamSha1(keyStream);

        return ModeDetector.InUnitTestRunner()
            ? Disposable.Create(() => { })
            : new SingleGlobalInstance(keyHash, TimeSpan.FromMilliseconds(2000));
    }
}