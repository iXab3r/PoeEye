using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using log4net;
using PoeShared.Squirrel.Scaffolding;
using Splat;
using Squirrel;

namespace PoeShared.Squirrel.Core
{
    public sealed partial class PoeUpdateManager
    {
        private class CheckForUpdateImpl : IEnableLogger
        {
            private static readonly ILog Log = LogManager.GetLogger(typeof(CheckForUpdateImpl));

            private readonly string rootAppDirectory;

            public CheckForUpdateImpl(string rootAppDirectory)
            {
                this.rootAppDirectory = rootAppDirectory;
            }

            public async Task<UpdateInfo> CheckForUpdate(
                string localReleaseFile,
                string updateUrlOrPath,
                bool ignoreDeltaUpdates = false,
                Action<int> progress = null,
                IFileDownloader urlDownloader = null)
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

                string releaseFile;

                var latestLocalRelease = localReleases.Any()
                    ? localReleases.MaxBy(x => x.Version).First()
                    : default;

                // Fetch the remote RELEASES file, whether it's a local dir or an
                // HTTP URL
                if (Utility.IsHttpUrl(updateUrlOrPath))
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
                        releaseFile = Encoding.UTF8.GetString(data);
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

                    progress(33);
                }
                else
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

                    releaseFile = await File.ReadAllTextAsync(fi.FullName, Encoding.UTF8);
                    progress(33);
                }

                var remoteReleases = ReleaseEntry.ParseReleaseFileAndApplyStaging(releaseFile, stagingId).ToArray();
                progress(66);

                if (!remoteReleases.Any())
                {
                    throw new Exception("Remote release File is empty or corrupted");
                }

                var ret = DetermineUpdateInfo(localReleases, remoteReleases, ignoreDeltaUpdates);

                progress(100);
                return ret;
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

            private UpdateInfo DetermineUpdateInfo(ReleaseEntry[] localReleases, ReleaseEntry[] remoteReleases, bool ignoreDeltaUpdates)
            {
                var packageDirectory = Utility.PackageDirectoryForAppDir(rootAppDirectory);
                localReleases ??= Array.Empty<ReleaseEntry>();

                if (remoteReleases == null)
                {
                    Log.WarnFormat("Release information couldn't be determined due to remote corrupt RELEASES file");
                    throw new Exception("Corrupt remote RELEASES file");
                }

                var latestFullRelease = Utility.FindCurrentVersion(remoteReleases);
                var currentRelease = Utility.FindCurrentVersion(localReleases);

                if (latestFullRelease == currentRelease)
                {
                    Log.InfoFormat("No updates, remote and local are the same");

                    var info = UpdateInfo.Create(currentRelease, new[] {latestFullRelease}, packageDirectory);
                    return info;
                }

                if (ignoreDeltaUpdates)
                {
                    remoteReleases = remoteReleases.Where(x => !x.IsDelta).ToArray();
                }

                if (!localReleases.Any())
                {
                    Log.WarnFormat("First run or local directory is corrupt, starting from scratch");
                    return UpdateInfo.Create(null, new[] {latestFullRelease}, packageDirectory);
                }

                if (localReleases.Max(x => x.Version) > remoteReleases.Max(x => x.Version))
                {
                    Log.WarnFormat("hwhat, local version is greater than remote version");
                    return UpdateInfo.Create(Utility.FindCurrentVersion(localReleases), new[] {latestFullRelease}, packageDirectory);
                }

                return UpdateInfo.Create(currentRelease, remoteReleases, packageDirectory);
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

                    Log.InfoFormat("Using existing staging user ID: {0}", ret.ToString());
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
                    Log.InfoFormat("Generated new staging user ID: {0}", ret.ToString());
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
}