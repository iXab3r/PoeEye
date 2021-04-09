using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using log4net;
using PoeShared.Scaffolding;
using PoeShared.Squirrel.Scaffolding;
using Splat;
using Squirrel;

namespace PoeShared.Squirrel.Core
{
    internal class CheckForUpdateImpl : IEnableLogger
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CheckForUpdateImpl));

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
            Action<int> progress = null)
        {
            progress ??= _ => { };

            var localReleases = Array.Empty<ReleaseEntry>();
            var stagingId = GetOrCreateStagedUserId();

            var shouldInitialize = false;
            try
            {
                localReleases = Utility.LoadLocalReleases(localReleaseFile).ToArray();
            }
            catch (Exception ex)
            {
                // Something has gone pear-shaped, let's start from scratch
                Log.Warn("Failed to load local releases, starting from scratch", ex);
                shouldInitialize = true;
            }

            if (shouldInitialize)
            {
                await InitializeClientAppDirectory();
            }


            var latestLocalRelease = localReleases.Any()
                ? localReleases.MaxBy(x => x.Version).First()
                : default;

            var isRemoteRepository = Utility.IsHttpUrl(updateUrlOrPath);

            // Fetch the remote RELEASES file, whether it's a local dir or an
            string releaseFile;
            if (isRemoteRepository)
            {
                releaseFile = await LoadRemoteReleases(updateUrlOrPath, urlDownloader, latestLocalRelease);
            }
            else
            {
                releaseFile = await LoadLocalReleases(updateUrlOrPath);
            }
            progress(33);

            var parsedReleases = ReleaseEntry.ParseReleaseFileAndApplyStaging(releaseFile, stagingId).ToArray();
            progress(55);

            if (!parsedReleases.Any())
            {
                throw new Exception("Remote release File is empty or corrupted");
            }

            Log.Debug($"Remote releases: \n\t{parsedReleases.DumpToTable()}");
            var result = DetermineUpdateInfo(localReleases, parsedReleases, ignoreDeltaUpdates);

            progress(100);
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

            Log.InfoFormat("Downloading RELEASES file from {0}", updateUrlOrPath);

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
            Log.InfoFormat("Reading RELEASES file from {0}", updateUrlOrPath);

            if (!Directory.Exists(updateUrlOrPath))
            {
                var message = $"The directory {updateUrlOrPath} does not exist, something is probably broken with your application";

                throw new Exception(message);
            }

            var fi = new FileInfo(Path.Combine(updateUrlOrPath, "RELEASES"));
            if (!fi.Exists)
            {
                var message = $"The file {fi.FullName} does not exist, something is probably broken with your application";

                Log.WarnFormat(message);

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

        private IPoeUpdateInfo DetermineUpdateInfo(
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
            localReleases ??= Array.Empty<ReleaseEntry>();

            if (remoteReleases == null || remoteReleases.Count == 0)
            {
                Log.Warn("Release information couldn't be determined due to remote corrupt RELEASES file");
                throw new Exception("Corrupted remote RELEASES file");
            }

            var latestFullRelease = Utility.FindCurrentVersion(remoteReleases);
            var currentRelease = Utility.FindCurrentVersion(localReleases);

            if (latestFullRelease == currentRelease)
            {
                Log.Info("No updates, remote and local are the same");
                return PoeUpdateInfo.Create(currentRelease, new[] {latestFullRelease}, packageDirectory);
            }

            var latestLocal =
                localReleases.Any() ? localReleases.OrderByDescending(x => x.Version).FirstOrDefault() : default;
            if (latestLocal == null)
            {
                Log.Warn("First run or local directory is corrupt, starting from scratch");
                return PoeUpdateInfo.Create(null, new[] {latestFullRelease}, packageDirectory);
            }

            var latestRemote = remoteReleases.OrderByDescending(x => x.Version).First();
            if (latestRemote.Version < latestLocal.Version)
            {
                Log.Warn($"Local release {latestLocal.DumpToTextRaw()} is greater than remote release {latestRemote.DumpToTextRaw()}");
                return PoeUpdateInfo.Create(currentRelease, new[] {latestFullRelease}, packageDirectory);
            }

            return PoeUpdateInfo.Create(currentRelease, remoteReleases, packageDirectory);
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
                Log.Debug("Couldn't read staging user ID, creating a blank one", ex);
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