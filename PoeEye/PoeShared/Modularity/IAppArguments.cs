using JetBrains.Annotations;

namespace PoeShared.Modularity;

public interface IAppConfig
{
    string AppName { [NotNull] get; }
        
    string AppTitle { [NotNull] get; }
        
    Version Version { get; }

    /// <summary>
    /// Equivalent of AppDomain.CurrentDomain.BaseDirectory
    /// </summary>
    string AppDomainDirectory { [CanBeNull] get; }

    /// <summary>
    /// "Profile" subfolder in RoamingAppDataDirectory
    /// </summary>
    string AppDataDirectory { [CanBeNull] get; }
    
    /// <summary>
    /// "Temp" subfolder in AppDataDirectory
    /// </summary>
    string TempDirectory { [CanBeNull] get; }

    /// <summary>
    /// "AppName" subfolder either in %appdata% (roaming) or "data" folder
    /// </summary>
    string RoamingAppDataDirectory { [CanBeNull] get; }
    
    /// <summary>
    /// "AppName" subfolder in %localappdata% or appdomain if "data" folder is specified
    /// </summary>
    string LocalAppDataDirectory { [CanBeNull] get; }
    
    /// <summary>
    /// Current process Id, equivalent of Environment.ProcessId, but for older frameworks
    /// </summary>
    int ProcessId { get; }

    string ApplicationExecutableName { [CanBeNull] get; }
    
    string ApplicationExecutablePath { [CanBeNull] get; }
    
    DirectoryInfo EnvironmentLocalAppData { get; }
    
    DirectoryInfo EnvironmentAppData { get; }
}

public interface IAppArguments : IAppConfig
{
    string Profile { get; }
    
    string DataFolder { get; }
    
    bool IsDebugMode { get; }
        
    bool IsLazyMode { get; }
        
    bool ShowUpdater { get; }
    
    bool? IsSafeMode { get; }
    
    bool? IsAdminMode { get; }
        
    IEnumerable<string> PrismModules { get; }
        
    bool IsElevated { get; }
        
    string AutostartFlag { [CanBeNull] get; }
        
    string StartupArgs { [CanBeNull] get; }
    
    string[] CommandLineArguments { [CanBeNull] get; }

    bool Parse(string[] args);

    string[] ParseCommandLineArguments(string commandLine);
}