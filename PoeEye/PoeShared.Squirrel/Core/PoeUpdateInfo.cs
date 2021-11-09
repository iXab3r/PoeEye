using System;
using System.Collections.Generic;
using System.Linq;
using NuGet;
using PoeShared.Squirrel.Scaffolding;
using Squirrel;

namespace PoeShared.Squirrel.Core
{
    internal sealed class PoeUpdateInfo : IPoeUpdateInfo
    {
        private PoeUpdateInfo(
            IReleaseEntry currentlyInstalledVersion,
            IReadOnlyCollection<IReleaseEntry> localReleases,
            IReadOnlyCollection<IReleaseEntry> remoteReleases,
            IReadOnlyCollection<IReleaseEntry> releasesToApply,
            string packageDirectory)
        {
            
            CurrentlyInstalledVersion = currentlyInstalledVersion;
            ReleasesToApply = releasesToApply ?? throw new ArgumentNullException(nameof(releasesToApply));
            LocalReleases = localReleases ?? throw new ArgumentNullException(nameof(localReleases));
            RemoteReleases = remoteReleases ?? throw new ArgumentNullException(nameof(remoteReleases));
            FutureReleaseEntry = releasesToApply.Any() ? ReleasesToApply.OrderByDescending(x => x.Version).FirstOrDefault() : CurrentlyInstalledVersion;
            PackageDirectory = packageDirectory;
            IsDelta = releasesToApply.Any(x => x.IsDelta);
        }

        public IReleaseEntry CurrentlyInstalledVersion { get; }
        
        public IReadOnlyCollection<IReleaseEntry> LocalReleases { get; }
        
        public IReadOnlyCollection<IReleaseEntry> RemoteReleases { get; }

        public IReleaseEntry FutureReleaseEntry { get; }

        public IReadOnlyCollection<IReleaseEntry> ReleasesToApply { get; }

        public bool IsDelta { get; }

        public string PackageDirectory { get; }

        public static IPoeUpdateInfo Create(
            IReleaseEntry currentVersion,
            IReadOnlyCollection<IReleaseEntry> localReleases,
            IReadOnlyCollection<IReleaseEntry> remoteReleases,
            string packageDirectory)
        {
            var latestFullRelease = remoteReleases.Where(x => !x.IsDelta).OrderByDescending(x => x.Version).FirstOrDefault() ?? throw new Exception("There should always be at least one full release");
            if (currentVersion == null)
            {
                // local version is not installed - downloading latest full version
                return new PoeUpdateInfo(null,  localReleases, remoteReleases, new[] { latestFullRelease }, packageDirectory);
            }
            
            if (currentVersion.Version >= latestFullRelease.Version)
            {
                // installed version is greater than remote
                return new PoeUpdateInfo(currentVersion, localReleases, remoteReleases, Array.Empty<IReleaseEntry>(), packageDirectory);
            }

            var source = remoteReleases.Where(x => x.Version > currentVersion.Version).OrderBy(v => v.Version).ToArray();
            var totalDeltaReleasesSize = source.Where(x => x.IsDelta).Sum(x => x.Filesize);
            if (totalDeltaReleasesSize > 0L && totalDeltaReleasesSize < latestFullRelease.Filesize)
            {
                // applying delta-updates
                return new PoeUpdateInfo(currentVersion, localReleases, remoteReleases, source.Where(x => x.IsDelta).ToArray(), packageDirectory);
            }
            return new PoeUpdateInfo(currentVersion, localReleases, remoteReleases, new[] { latestFullRelease }, packageDirectory);
        }

        public override string ToString()
        {
            return $"UpdateInfo {{ isDelta:{IsDelta}, current: {CurrentlyInstalledVersion}, releasesToApply: {ReleasesToApply.Count} }}";
        }
    }
}