using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace PoeShared.Modularity
{
    public interface IAppConfig
    {
        string AppName { [NotNull] get; }
        
        string AppTitle { [NotNull] get; }
        
        Version Version { get; }
        
        string AppDataDirectory { [CanBeNull] get; }
        
        int ProcessId { get; }

        string ApplicationExecutableName { [CanBeNull] get; }
        
        DirectoryInfo ApplicationDirectory { get; }
    }

    public interface IAppArguments : IAppConfig
    {
        bool IsDebugMode { get; }
        
        bool IsLazyMode { get; }
        
        bool ShowUpdater { get; }
        
        IEnumerable<string> PrismModules { get; }
        
        bool IsElevated { get; }
        
        string AutostartFlag { [CanBeNull] get; }
        
        string StartupArgs { [CanBeNull] get; }

        bool Parse(string[] args);
    }
}