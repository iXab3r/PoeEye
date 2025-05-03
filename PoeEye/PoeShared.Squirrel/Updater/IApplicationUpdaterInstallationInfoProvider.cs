using System;
using System.IO;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeShared.Squirrel.Updater;

public interface IApplicationUpdaterInstallationInfoProvider
{
    public DirectoryInfo RootDirectory { get; }
    
    public FileInfo RunningExecutable { get; }
    
    public FileInfo LauncherExecutable { get; }

    public DirectoryInfo AppRootDirectory { get; }
    
    public bool IsSquirrel { get; }
}