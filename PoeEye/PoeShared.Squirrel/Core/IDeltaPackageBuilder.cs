using ReleasePackage = PoeShared.Squirrel.Core.ReleasePackage;

namespace PoeShared.Squirrel.Core;

public interface IDeltaPackageBuilder
{
    ReleasePackage ApplyDeltaPackage(ReleasePackage basePackage, ReleasePackage deltaPackage, string outputFile);
}