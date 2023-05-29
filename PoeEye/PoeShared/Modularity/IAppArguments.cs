using JetBrains.Annotations;

namespace PoeShared.Modularity;

public interface IAppConfig
{
    string AppName { [NotNull] get; }
        
    string AppTitle { [NotNull] get; }
        
    Version Version { get; }
        
    string AppDataDirectory { [CanBeNull] get; }
    
    string AppDomainDirectory { [CanBeNull] get; }
    
    string SharedAppDataDirectory { [CanBeNull] get; }
    
    string LocalAppDataDirectory { [CanBeNull] get; }
    
    int ProcessId { get; }

    string ApplicationExecutableName { [CanBeNull] get; }
    
    string ApplicationExecutablePath { [CanBeNull] get; }
    
    DirectoryInfo EnvironmentLocalAppData { get; }
    
    DirectoryInfo EnvironmentAppData { get; }
}

public interface IAppArguments : IAppConfig
{
    string Profile { get; }
    
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
}