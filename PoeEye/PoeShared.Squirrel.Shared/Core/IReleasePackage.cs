namespace PoeShared.Squirrel.Core;

public interface IReleasePackage
{
    string InputPackageFile { get; }
        
    string ReleasePackageFile { get; }
}