using NuGet;

namespace PoeShared.Squirrel.Scaffolding
{
    public static class VersionComparer
    {
        public static bool Matches(IVersionSpec versionSpec, SemanticVersion version)
        {
            if (versionSpec == null)
            {
                return true; // I CAN'T DEAL WITH THIS
            }

            bool minVersion;
            if (versionSpec.MinVersion == null)
            {
                minVersion = true; // no preconditon? LET'S DO IT
            }
            else if (versionSpec.IsMinInclusive)
            {
                minVersion = version >= versionSpec.MinVersion;
            }
            else
            {
                minVersion = version > versionSpec.MinVersion;
            }

            bool maxVersion;
            if (versionSpec.MaxVersion == null)
            {
                maxVersion = true; // no preconditon? LET'S DO IT
            }
            else if (versionSpec.IsMaxInclusive)
            {
                maxVersion = version <= versionSpec.MaxVersion;
            }
            else
            {
                maxVersion = version < versionSpec.MaxVersion;
            }

            return maxVersion && minVersion;
        }
    }
}