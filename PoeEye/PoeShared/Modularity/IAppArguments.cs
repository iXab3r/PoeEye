using JetBrains.Annotations;

namespace PoeShared.Modularity
{
    public interface IAppArguments
    {
        string AppName { [NotNull] get; }
        
        string AppDataDirectory { [CanBeNull] get; }
        
        bool IsDebugMode { get; }
        
        bool IsElevated { get; }
        
        int ProcessId { get; }
        
        string AutostartFlag { [CanBeNull] get; }
        
        string ApplicationExecutableName { [CanBeNull] get; }
        
        string StartupArgs { [CanBeNull] get; }
    }
}