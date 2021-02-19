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
        public IReleaseEntry CurrentlyInstalledVersion { get; }

        public IReleaseEntry FutureReleaseEntry { get; }

        public IReadOnlyCollection<IReleaseEntry> ReleasesToApply { get; }

        public bool IsDelta { get; }

        public string PackageDirectory { get; }
        
        private PoeUpdateInfo(
            IReleaseEntry currentlyInstalledVersion,
            IReadOnlyCollection<IReleaseEntry> releasesToApply,
            string packageDirectory)
        {
            CurrentlyInstalledVersion = currentlyInstalledVersion;
            ReleasesToApply = releasesToApply;
            FutureReleaseEntry = releasesToApply.Any() ? ReleasesToApply.OrderByDescending(x => x.Version).FirstOrDefault() : CurrentlyInstalledVersion;
            PackageDirectory = packageDirectory;
            IsDelta = releasesToApply.Any(x => x.IsDelta);
        }
        
        public static IPoeUpdateInfo Create(
            IReleaseEntry currentVersion,
            IReadOnlyCollection<IReleaseEntry> availableReleases,
            string packageDirectory)
        {
            var latestFullRelease = availableReleases.Where(x => !x.IsDelta).OrderByDescending(x => x.Version).FirstOrDefault() ?? throw new Exception("There should always be at least one full release");
            if (currentVersion == null)
            {
                // local version is not installed - downloading latest full version
                return new PoeUpdateInfo(null, new[] { latestFullRelease }, packageDirectory);
            }
            
            if (currentVersion.Version >= latestFullRelease.Version)
            {
                // installed version is greater than remote
                return new PoeUpdateInfo(currentVersion, Array.Empty<IReleaseEntry>(), packageDirectory);
            }

            var source = availableReleases.Where(x => x.Version > currentVersion.Version).OrderBy(v => v.Version).ToArray();
            var totalDeltaReleasesSize = source.Where(x => x.IsDelta).Sum(x => x.Filesize);
            if (totalDeltaReleasesSize > 0L && totalDeltaReleasesSize < latestFullRelease.Filesize)
            {
                // applying delta-updates
                return new PoeUpdateInfo(currentVersion, source.Where(x => x.IsDelta).ToArray(), packageDirectory);
            }
            return new PoeUpdateInfo(currentVersion, new[] { latestFullRelease }, packageDirectory);
        }
    }
}