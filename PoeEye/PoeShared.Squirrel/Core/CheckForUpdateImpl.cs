using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PoeShared.Scaffolding;
using PoeShared.Logging;
using PoeShared.Squirrel.Scaffolding;
using Splat;
using Squirrel;

namespace PoeShared.Squirrel.Core;

internal class CheckForUpdateImpl : IEnableLogger
{
    private static readonly IFluentLog Log = typeof(CheckForUpdateImpl).PrepareLogger();

    private readonly IFileDownloader urlDownloader;
    private readonly string rootAppDirectory;

    public CheckForUpdateImpl(
        IFileDownloader urlDownloader,
        string rootAppDirectory)
    {
        this.urlDownloader = urlDownloader;
        this.rootAppDirectory = rootAppDirectory;
    }
    
    public async Task<IPoeUpdateInfo> CheckForUpdate(
        string updateUrlOrPath,
        Version targetVersion,
        Action<int> progressCallback)
    {
        progressCallback ??= _ => { };
        var localReleases = Array.Empty<ReleaseEntry>();
        
        string releaseFile;
        if (Utility.IsHttpUrl(updateUrlOrPath))
        {
            Log.Debug($"Loading releases from remote path {updateUrlOrPath}");
            releaseFile = await LoadRemoteReleasesFromUrl(updateUrlOrPath, urlDownloader, latestLocalRelease: null, fileName: "RELEASES-LIST");
        }
        else
        {
            throw new NotSupportedException("Target version update is not supported for local releases");
        }
        progressCallback(33);
        var parsedReleases = ReleaseEntry.ParseReleaseFileAndApplyStaging(releaseFile, userToken: null).ToArray();
        progressCallback(55);

        var futureReleaseEntry = parsedReleases.FirstOrDefault(x => x.Version.Version == targetVersion);
        if (futureReleaseEntry == null)
        {
            throw new Exception($"Could not find release entry for version {targetVersion}, total entries: {parsedReleases.Length}");
        }

        var packageDirectory = Utility.PackageDirectoryForAppDir(rootAppDirectory);
        var futureUpdateInfo = PoeUpdateInfo.Create(currentVersion: null, localReleases, new[] { futureReleaseEntry }, packageDirectory);
        progressCallback(100);

        return futureUpdateInfo;
    }

    public async Task<IPoeUpdateInfo> CheckForUpdate(
        IReadOnlyList<IReleaseEntry> localReleases,
        string updateUrlOrPath,
        bool ignoreDeltaUpdates,
        Action<int> progressCallback)
    {
        progressCallback ??= _ => { };

        var latestLocalRelease = localReleases.Any()
            ? localReleases.MaxBy(x => x.Version)
            : default;

        // Fetch the remote RELEASES file, whether it's a local dir or an
        string releaseFile;
        if (Utility.IsHttpUrl(updateUrlOrPath))
        {
            Log.Debug($"Loading releases from remote path {updateUrlOrPath}");
            releaseFile = await LoadRemoteReleasesFromUrl(updateUrlOrPath, urlDownloader, latestLocalRelease);
        }
        else
        {
            Log.Debug($"Loading releases from local path {updateUrlOrPath}");
            releaseFile = await LoadRemoteReleasesFromFile(updateUrlOrPath);
        }

        progressCallback(33);

        var parsedReleases = ReleaseEntry.ParseReleaseFileAndApplyStaging(releaseFile, userToken: null).ToArray();
        progressCallback(55);

        if (!parsedReleases.Any())
        {
            throw new Exception("Remote release File is empty or corrupted");
        }

        if (Log.IsDebugEnabled)
        {
            const int maxReleasesToLog = 5;
            Log.Debug(parsedReleases.Length > maxReleasesToLog
                ? $"Remote releases(latest {maxReleasesToLog} of {parsedReleases.Length}): \n\t{parsedReleases.Select(x => new {x.PackageName, x.Filename, x.Filesize, x.Version}).OrderByDescending(x => x.Version).Take(maxReleasesToLog).DumpToString()}"
                : $"Remote releases({parsedReleases.Length}): \n\t{parsedReleases.Select(x => new {x.PackageName, x.Filename, x.Filesize, x.Version}).DumpToString()}");
        }

        var result = DetermineUpdateInfo(localReleases, parsedReleases, ignoreDeltaUpdates);

        progressCallback(100);
        return result;
    }

    public async Task<IPoeUpdateInfo> CheckForUpdate(
        string localReleaseFile,
        string updateUrlOrPath,
        bool ignoreDeltaUpdates = false,
        Action<int> progressCallback = null)
    {
        IReadOnlyList<IReleaseEntry> localReleases;

        bool shouldInitialize;
        try
        {
            localReleases = Utility.LoadLocalReleases(localReleaseFile);
            shouldInitialize = false;
        }
        catch (Exception ex)
        {
            // Something has gone pear-shaped, let's start from scratch
            if (ex is FileNotFoundException fileNotFoundException)
            {
                Log.Warn($"There are no local releases file {fileNotFoundException.FileName}, starting from scratch");
            }
            else
            {
                Log.Warn("Failed to load local releases, starting from scratch", ex);
            }

            localReleases = ArraySegment<ReleaseEntry>.Empty;
            shouldInitialize = true;
        }

        if (shouldInitialize)
        {
            Log.Debug("Initializing client app directory");
            await InitializeClientAppDirectory();
        }

        return await CheckForUpdate(localReleases, updateUrlOrPath, ignoreDeltaUpdates, progressCallback);
    }

    private static async Task<string> LoadRemoteReleasesFromUrl(
        string updateUrlOrPath,
        IFileDownloader urlDownloader,
        IReleaseEntry latestLocalRelease,
        string fileName)
    {
        if (updateUrlOrPath.EndsWith("/"))
        {
            updateUrlOrPath = updateUrlOrPath.Substring(0, updateUrlOrPath.Length - 1);
        }

        Log.Info($"Downloading {fileName} file from {updateUrlOrPath}");

        var retries = 3;

        retry:

        try
        {
            var uri = Utility.AppendPathToUri(new Uri(updateUrlOrPath), fileName);

            if (latestLocalRelease != null)
            {
                uri = Utility.AddQueryParamsToUri(
                    uri,
                    new Dictionary<string, string>
                    {
                        {"id", latestLocalRelease.PackageName},
                        {"localVersion", latestLocalRelease.Version.ToString()},
                        {
                            "arch", Environment.Is64BitOperatingSystem
                                ? "amd64"
                                : "x86"
                        }
                    });
            }

            var data = await urlDownloader.DownloadUrl(uri.ToString());
            return Encoding.UTF8.GetString(data);
        }
        catch (WebException ex)
        {
            Log.Info("Download resulted in WebException (returning blank release list)", ex);

            if (retries <= 0)
            {
                throw;
            }

            retries--;
            goto retry;
        }
    }

    private static async Task<string> LoadRemoteReleasesFromUrl(
        string updateUrlOrPath,
        IFileDownloader urlDownloader,
        IReleaseEntry latestLocalRelease)
    {
        return await LoadRemoteReleasesFromUrl(updateUrlOrPath, urlDownloader, latestLocalRelease, "RELEASES");
    }

    private static async Task<string> LoadRemoteReleasesFromFile(string updateUrlOrPath)
    {
        Log.Info($"Reading RELEASES file from {updateUrlOrPath}");

        if (!Directory.Exists(updateUrlOrPath))
        {
            var message = $"The directory {updateUrlOrPath} does not exist, something is probably broken with your application";

            throw new Exception(message);
        }

        var fi = new FileInfo(Path.Combine(updateUrlOrPath, "RELEASES"));
        if (!fi.Exists)
        {
            var message = $"The file {fi.FullName} does not exist, something is probably broken with your application";

            Log.Warn(message);

            var packages = Utility.EnumeratePackagesForApp(updateUrlOrPath);
            if (packages.Count == 0)
            {
                throw new Exception(message);
            }

            // NB: Create a new RELEASES file since we've got a directory of packages
            ReleaseEntry.WriteReleaseFile(
                packages.Select(x => ReleaseEntry.GenerateFromFile(x.FullName)),
                fi.FullName);
        }

        return await File.ReadAllTextAsync(fi.FullName, Encoding.UTF8);
    }

    private async Task InitializeClientAppDirectory()
    {
        // On bootstrap, we won't have any of our directories, create them
        var pkgDir = Path.Combine(rootAppDirectory, "packages");
        if (Directory.Exists(pkgDir))
        {
            await Utility.DeleteDirectory(pkgDir);
        }

        Directory.CreateDirectory(pkgDir);
    }

    public IPoeUpdateInfo DetermineUpdateInfo(
        IReadOnlyCollection<IReleaseEntry> localReleases,
        IReadOnlyCollection<IReleaseEntry> remoteReleases,
        bool ignoreDeltaUpdates)
    {
        if (ignoreDeltaUpdates)
        {
            remoteReleases = remoteReleases.Where(x => !x.IsDelta).ToArray();
        }

        return DetermineUpdateInfo(localReleases, remoteReleases);
    }

    private IPoeUpdateInfo DetermineUpdateInfo(
        IReadOnlyCollection<IReleaseEntry> localReleases,
        IReadOnlyCollection<IReleaseEntry> remoteReleases)
    {
        if (remoteReleases == null || remoteReleases.Count == 0)
        {
            Log.Warn("Release information couldn't be determined due to corrupted remote RELEASES file");
            throw new Exception("Corrupted remote RELEASES file");
        }

        IReleaseEntry currentRelease;
        var entryAssembly = Assembly.GetEntryAssembly();
        var entryVersion = entryAssembly?.GetName()?.Version;
        Log.Info($"Determining local version, entry point: {entryAssembly} v{entryVersion}");
        if (localReleases.IsEmpty() && remoteReleases.Any() && entryAssembly != null)
        {
            Log.Warn($"No local releases - trying to guess by entry assembly version: {entryAssembly} v{entryAssembly.GetName().Version}");
            currentRelease = remoteReleases.FirstOrDefault(x => x.Version.Version.Build == entryAssembly.GetName().Version?.Build);
            if (currentRelease == null)
            {
                Log.Warn($"Failed to find remote release matching entry point version {entryVersion}, remote releases:\n\t{remoteReleases.Select(x => new { x.PackageName, x.Filename, x.Version }).DumpToTable()}");
            }
        }
        else
        {
            currentRelease = Utility.FindCurrentVersion(localReleases);
            if (currentRelease == null)
            {
                Log.Warn($"Failed to find local release, local version:\n\t{localReleases.Select(x => new { x.PackageName, x.Filename, x.Version }).DumpToTable()}");
            }
        }

        Log.Info($"Current release: {currentRelease}");
        var packageDirectory = Utility.PackageDirectoryForAppDir(rootAppDirectory);
        return PoeUpdateInfo.Create(currentRelease, localReleases, remoteReleases, packageDirectory);
    }

    private Guid? GetOrCreateStagedUserId()
    {
        var stagedUserIdFile = Path.Combine(rootAppDirectory, "packages", ".betaId");
        Guid ret;

        try
        {
            if (!Guid.TryParse(File.ReadAllText(stagedUserIdFile, Encoding.UTF8), out ret))
            {
                throw new Exception("File was read but contents were invalid");
            }

            Log.Info($"Using existing staging user ID: {ret.ToString()}");
            return ret;
        }
        catch (Exception ex)
        {
            if (ex is FileNotFoundException fileNotFoundException)
            {
                Log.Warn($"There are no staging user ID file {fileNotFoundException.FileName}, creating a blank one");
            }
            else
            {
                Log.Warn("Couldn't read staging user ID, creating a blank one", ex);
            }
        }

        var prng = new Random();
        var buf = new byte[4096];
        prng.NextBytes(buf);

        ret = Utility.CreateGuidFromHash(buf);
        try
        {
            File.WriteAllText(stagedUserIdFile, ret.ToString(), Encoding.UTF8);
            Log.Info($"Generated new staging user ID: {ret}");
            return ret;
        }
        catch (Exception ex)
        {
            Log.Warn("Couldn't write out staging user ID, this user probably shouldn't get beta anything", ex);
            return null;
        }
    }
}