using System;

namespace PoeShared.Squirrel.Scaffolding
{
    public interface IReleasePackage
    {
        string InputPackageFile { get; }
        
        string ReleasePackageFile { get; }
        
        string SuggestedReleaseFileName { get; }
    }
}