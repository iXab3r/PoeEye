using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PoeShared.Scaffolding; 
using PoeShared.Logging; 
using PoeShared.Squirrel.Scaffolding;
using Splat;
using Squirrel;

namespace PoeShared.Squirrel.Core
{
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
            string localReleaseFile,
            string updateUrlOrPath,
            bool ignoreDeltaUpdates = false,
            Action<int> progressCallback = null)
        {
            progressCallback ??= _ => { };

            IReadOnlyCollection<ReleaseEntry> localReleases;
            var stagingId = GetOrCreateStagedUserId();

            bool shouldInitialize;
            try
            {
                localReleases = Utility.LoadLocalReleases(localReleaseFile).ToArray();
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

            var latestLocalRelease = localReleases.Any()
                ? localReleases.MaxBy(x => x.Version).First()
                : default;

            // Fetch the remote RELEASES file, whether it's a local dir or an
            string releaseFile;
            if (Utility.IsHttpUrl(updateUrlOrPath))
            {
                Log.Debug($"Loading releases from remote path {updateUrlOrPath}");
                releaseFile = await LoadRemoteReleases(updateUrlOrPath, urlDownloader, latestLocalRelease);
            }
            else
            {
                Log.Debug($"Loading releases from local path {updateUrlOrPath}");
                releaseFile = await LoadLocalReleases(updateUrlOrPath);
            }
            progressCallback(33);

            var parsedReleases = ReleaseEntry.ParseReleaseFileAndApplyStaging(releaseFile, stagingId).ToArray();
            progressCallback(55);

            if (!parsedReleases.Any())
            {
                throw new Exception("Remote release File is empty or corrupted");
            }

            if (Log.IsDebugEnabled)
            {
                const int maxReleasesToLog = 5;
                Log.Debug(parsedReleases.Length > maxReleasesToLog
                    ? $"Remote releases(latest {maxReleasesToLog} of {parsedReleases.Length}): \n\t{parsedReleases.OrderByDescending(x => x.Version).Take(maxReleasesToLog).DumpToString()}"
                    : $"Remote releases({parsedReleases.Length}): \n\t{parsedReleases.DumpToString()}");
            }
            
            var result = DetermineUpdateInfo(localReleases, parsedReleases, ignoreDeltaUpdates);

            progressCallback(100);
            return result;
        }

        private static async Task<string> LoadRemoteReleases(
            string updateUrlOrPath, 
            IFileDownloader urlDownloader,
            IReleaseEntry latestLocalRelease)
        {
            if (updateUrlOrPath.EndsWith("/"))
            {
                updateUrlOrPath = updateUrlOrPath.Substring(0, updateUrlOrPath.Length - 1);
            }

            Log.Info($"Downloading RELEASES file from {updateUrlOrPath}");

            var retries = 3;

            retry:

            try
            {
                var uri = Utility.AppendPathToUri(new Uri(updateUrlOrPath), "RELEASES");

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
            
        private static async Task<string> LoadLocalReleases(string updateUrlOrPath)
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

                var packages = new DirectoryInfo(updateUrlOrPath).GetFiles("*.nupkg");
                if (packages.Length == 0)
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
            var packageDirectory = Utility.PackageDirectoryForAppDir(rootAppDirectory);

            if (remoteReleases == null || remoteReleases.Count == 0)
            {
                Log.Warn("Release information couldn't be determined due to remote corrupt RELEASES file");
                throw new Exception("Corrupted remote RELEASES file");
            }

            var currentRelease = Utility.FindCurrentVersion(localReleases);
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
}