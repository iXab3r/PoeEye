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
        public IReleaseEntry CurrentlyInstalledVersion { get; init; }

        public IReleaseEntry FutureReleaseEntry { get; init; }

        public IReadOnlyCollection<IReleaseEntry> ReleasesToApply { get; }

        public string PackageDirectory { get; }
        
        private PoeUpdateInfo(
            IReleaseEntry currentlyInstalledVersion,
            IReadOnlyCollection<IReleaseEntry> releasesToApply,
            string packageDirectory)
        {
            CurrentlyInstalledVersion = currentlyInstalledVersion;
            ReleasesToApply = (releasesToApply ?? Enumerable.Empty<IReleaseEntry>()).ToList();
            FutureReleaseEntry = ReleasesToApply.Any() ? ReleasesToApply.MaxBy(x => x.Version).FirstOrDefault() : CurrentlyInstalledVersion;
            PackageDirectory = packageDirectory;
        }
        
        public static IPoeUpdateInfo Create(
            IReleaseEntry currentVersion,
            IReadOnlyCollection<IReleaseEntry> availableReleases,
            string packageDirectory)
        {
            var releaseEntry = availableReleases.MaxBy(x => x.Version).FirstOrDefault(x => !x.IsDelta);
            if (releaseEntry == null)
                throw new Exception("There should always be at least one full release");
            if (currentVersion == null)
                return new PoeUpdateInfo(null, new IReleaseEntry[]
                {
                    releaseEntry
                }, packageDirectory);
            if (currentVersion.Version >= releaseEntry.Version)
            {
                return new PoeUpdateInfo(currentVersion, Array.Empty<IReleaseEntry>(), packageDirectory);
            }

            var source = availableReleases.Where(x => x.Version > currentVersion.Version).OrderBy(v => v.Version).ToArray();
            var totalUpdateSize = source.Where(x => x.IsDelta).Sum(x => x.Filesize);
            if (totalUpdateSize < releaseEntry.Filesize && totalUpdateSize > 0L)
            {
                return new PoeUpdateInfo(currentVersion, source.Where(x => x.IsDelta).ToArray(), packageDirectory);
            }
            return new PoeUpdateInfo(currentVersion, new[]
            {
                releaseEntry
            }, packageDirectory);
        }
    }
}