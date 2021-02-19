using System.Collections.Generic;
using Squirrel;

namespace PoeShared.Squirrel.Core
{
    public interface IPoeUpdateInfo
    {
        public IReleaseEntry CurrentlyInstalledVersion { get; }

        public IReleaseEntry FutureReleaseEntry { get;  }

        public IReadOnlyCollection<IReleaseEntry> ReleasesToApply { get; }

        public bool IsDelta { get; }

        public string PackageDirectory { get; }
    }
}