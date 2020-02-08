using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using PoeShared.Squirrel.Scaffolding;
using Splat;
using Squirrel;

namespace PoeShared.Squirrel.Core
{
    public sealed partial class PoeUpdateManager
    {
        internal class DownloadReleasesImpl : IEnableLogger
        {
            private static readonly ILog Log = LogManager.GetLogger(typeof(DownloadReleasesImpl));

            private readonly string rootAppDirectory;

            public DownloadReleasesImpl(string rootAppDirectory)
            {
                this.rootAppDirectory = rootAppDirectory;
            }

            public async Task DownloadReleases(string updateUrlOrPath, IEnumerable<ReleaseEntry> releasesToDownload, Action<int> progress = null,
                IFileDownloader urlDownloader = null)
            {
                progress = progress ?? (_ => { });
                urlDownloader = urlDownloader ?? new FileDownloader();
                var packagesDirectory = Path.Combine(rootAppDirectory, "packages");

                double current = 0;
                var toIncrement = 100.0 / releasesToDownload.Count();

                if (Utility.IsHttpUrl(updateUrlOrPath))
                {
                    // From Internet
                    await releasesToDownload.ForEachAsync(
                        async x =>
                        {
                            var targetFile = Path.Combine(packagesDirectory, x.Filename);
                            double component = 0;
                            await DownloadRelease(
                                updateUrlOrPath,
                                x,
                                urlDownloader,
                                targetFile,
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
                        });
                }
                else
                {
                    // From Disk
                    await releasesToDownload.ForEachAsync(
                        x =>
                        {
                            var targetFile = Path.Combine(packagesDirectory, x.Filename);

                            File.Copy(
                                Path.Combine(updateUrlOrPath, x.Filename),
                                targetFile,
                                true);

                            lock (progress)
                            {
                                progress((int) Math.Round(current += toIncrement));
                            }

                            ChecksumPackage(x);
                        });
                }
            }

            private bool IsReleaseExplicitlyHttp(ReleaseEntry x)
            {
                return x.BaseUrl != null &&
                       Uri.IsWellFormedUriString(x.BaseUrl, UriKind.Absolute);
            }

            private Task DownloadRelease(string updateBaseUrl, ReleaseEntry releaseEntry, IFileDownloader urlDownloader, string targetFile,
                Action<int> progress)
            {
                var baseUri = Utility.EnsureTrailingSlash(new Uri(updateBaseUrl));

                var releaseEntryUrl = releaseEntry.BaseUrl + releaseEntry.Filename;
                if (!string.IsNullOrEmpty(releaseEntry.Query))
                {
                    releaseEntryUrl += releaseEntry.Query;
                }

                var sourceFileUrl = new Uri(baseUri, releaseEntryUrl).AbsoluteUri;
                File.Delete(targetFile);

                return urlDownloader.DownloadFile(sourceFileUrl, targetFile, progress);
            }

            private Task ChecksumAllPackages(IEnumerable<ReleaseEntry> releasesDownloaded)
            {
                return releasesDownloaded.ForEachAsync(x => ChecksumPackage(x));
            }

            private void ChecksumPackage(ReleaseEntry downloadedRelease)
            {
                var targetPackage = new FileInfo(
                    Path.Combine(rootAppDirectory, "packages", downloadedRelease.Filename));

                if (!targetPackage.Exists)
                {
                    Log.ErrorFormat("File {0} should exist but doesn't", targetPackage.FullName);

                    throw new Exception("Checksummed file doesn't exist: " + targetPackage.FullName);
                }

                if (targetPackage.Length != downloadedRelease.Filesize)
                {
                    Log.ErrorFormat("File Length should be {0}, is {1}", downloadedRelease.Filesize, targetPackage.Length);
                    targetPackage.Delete();

                    throw new Exception("Checksummed file size doesn't match: " + targetPackage.FullName);
                }

                using (var file = targetPackage.OpenRead())
                {
                    var hash = Utility.CalculateStreamSha1(file);

                    if (!hash.Equals(downloadedRelease.SHA1, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.ErrorFormat("File SHA1 should be {0}, is {1}", downloadedRelease.SHA1, hash);
                        targetPackage.Delete();
                        throw new Exception("Checksum doesn't match: " + targetPackage.FullName);
                    }
                }
            }
        }
    }
}