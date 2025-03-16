using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using NuGet;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Squirrel.Scaffolding;
using Splat;
using Squirrel;
using Squirrel.Shell;

namespace PoeShared.Squirrel.Core;

public sealed partial class PoeUpdateManager : DisposableReactiveObject, IPoeUpdateManager
{
    private static readonly IFluentLog Log = typeof(PoeUpdateManager).PrepareLogger();

    private readonly IFileDownloader urlDownloader;

    public PoeUpdateManager(
        string urlOrPath,
        IFileDownloader urlDownloader,
        string applicationName,
        string rootDirectory)
    {
        Guard.ArgumentIsTrue(!string.IsNullOrEmpty(urlOrPath), "!string.IsNullOrEmpty(urlOrPath)");
        Guard.ArgumentIsTrue(!string.IsNullOrEmpty(applicationName), "!string.IsNullOrEmpty(applicationName)");

        UpdateUrlOrPath = urlOrPath;
        this.urlDownloader = urlDownloader;
        ApplicationName = applicationName ?? GetApplicationName();
        RootAppDirectory = Path.Combine(rootDirectory, ApplicationName);
    }
    
    public string UpdateUrlOrPath { get; }

    public string ApplicationName { get; }

    public string RootAppDirectory { get; }

    public async Task<IPoeUpdateInfo> PrepareUpdate(
        bool ignoreDeltaUpdates, 
        IReadOnlyCollection<IReleaseEntry> localReleases, 
        IReadOnlyCollection<IReleaseEntry> remoteReleases)
    {
        var checkForUpdate = new CheckForUpdateImpl(urlDownloader, RootAppDirectory);
        return checkForUpdate.DetermineUpdateInfo(localReleases, remoteReleases, ignoreDeltaUpdates);
    }

    public async Task<IPoeUpdateInfo> CheckForUpdate(bool ignoreDeltaUpdates, Action<int> progress = null)
    {
        using var sw = new BenchmarkTimer($"Checking for updates, ignore delta: {ignoreDeltaUpdates}", Log);
        using var updateLock = AcquireUpdateLock();
            
        sw.Step("Update lock acquired");
        var checkForUpdate = new CheckForUpdateImpl(urlDownloader, RootAppDirectory);
        var localReleasePath = Utility.LocalReleaseFileForAppDir(RootAppDirectory);
        var result = await checkForUpdate.CheckForUpdate(
            localReleasePath,
            UpdateUrlOrPath,
            ignoreDeltaUpdates,
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

    public Task<RegistryKey> CreateUninstallerRegistryEntry(string uninstallCmd, string quietSwitch)
    {
        var installHelpers = new InstallHelperImpl(ApplicationName, RootAppDirectory);
        return installHelpers.CreateUninstallerRegistryEntry(uninstallCmd, quietSwitch);
    }

    public Task<RegistryKey> CreateUninstallerRegistryEntry()
    {
        var installHelpers = new InstallHelperImpl(ApplicationName, RootAppDirectory);
        return installHelpers.CreateUninstallerRegistryEntry();
    }

    public void RemoveUninstallerRegistryEntry()
    {
        var installHelpers = new InstallHelperImpl(ApplicationName, RootAppDirectory);
        installHelpers.RemoveUninstallerRegistryEntry();
    }

    public void CreateShortcutsForExecutable(string exeName, ShortcutLocation locations, bool updateOnly, string programArguments = null,
        string icon = null)
    {
        Log.Debug($"Setting up shortcuts for {exeName} in {locations}, updateOnly: {updateOnly}, args: {programArguments}");
        var installHelpers = new ApplyReleasesImpl(RootAppDirectory);
        installHelpers.CreateShortcutsForExecutable(exeName, locations, updateOnly, programArguments, icon);
    }

    public void RemoveShortcutsForExecutable(string exeName, ShortcutLocation locations)
    {
        Log.Debug($"Removing shortcuts for {exeName} in {locations}");
        var installHelpers = new ApplyReleasesImpl(RootAppDirectory);
        installHelpers.RemoveShortcutsForExecutable(exeName, locations);
    }

    public SemanticVersion CurrentlyInstalledVersion(string executable = null)
    {
        executable ??= Path.GetDirectoryName(typeof(PoeUpdateManager).Assembly.Location);

        if (!executable.StartsWith(RootAppDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var appDirName = executable.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .FirstOrDefault(x => x.StartsWith("app-", StringComparison.OrdinalIgnoreCase));

        return appDirName?.ToSemanticVersion();
    }
        
    public Dictionary<ShortcutLocation, ShellLink> GetShortcutsForExecutable(string exeName, ShortcutLocation locations, string programArguments = null)
    {
        var installHelpers = new ApplyReleasesImpl(RootAppDirectory);
        return installHelpers.GetShortcutsForExecutable(exeName, locations, programArguments);
    }

    public void KillAllExecutablesBelongingToPackage()
    {
        var installHelpers = new InstallHelperImpl(ApplicationName, RootAppDirectory);
        installHelpers.KillAllProcessesBelongingToPackage();
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

    private static string GetApplicationName()
    {
        var fi = new FileInfo(GetUpdateExe());
        return fi.Directory.Name;
    }

    private static string GetUpdateExe()
    {
        var assembly = Assembly.GetEntryAssembly();

        // Are we update.exe?
        if (assembly != null &&
            Path.GetFileName(assembly.Location).Equals("update.exe", StringComparison.OrdinalIgnoreCase) &&
            assembly.Location.IndexOf("app-", StringComparison.OrdinalIgnoreCase) == -1 &&
            assembly.Location.IndexOf("SquirrelTemp", StringComparison.OrdinalIgnoreCase) == -1)
        {
            return Path.GetFullPath(assembly.Location);
        }

        assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        var updateDotExe = Path.Combine(Path.GetDirectoryName(assembly.Location), "..\\Update.exe");
        var target = new FileInfo(updateDotExe);

        if (!target.Exists)
        {
            throw new Exception("Update.exe not found, not a Squirrel-installed app?");
        }

        return target.FullName;
    }
}