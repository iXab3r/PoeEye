using JetBrains.Annotations;

namespace PoeShared.Modularity
{
    public interface IAppConfig
    {
        string AppName { [NotNull] get; }
        
        string AppDataDirectory { [CanBeNull] get; }
        
        int ProcessId { get; }

        string ApplicationExecutableName { [CanBeNull] get; }
    }

    public interface IAppArguments : IAppConfig
    {
        bool IsDebugMode { get; }
        
        bool IsElevated { get; }
        
        string AutostartFlag { [CanBeNull] get; }
        
        string StartupArgs { [CanBeNull] get; }
    }
}