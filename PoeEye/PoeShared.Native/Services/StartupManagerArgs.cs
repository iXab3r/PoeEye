using JetBrains.Annotations;

namespace PoeShared.Services;

public sealed class StartupManagerArgs
{
    public string UniqueAppName { [CanBeNull] get; [CanBeNull] set; }
        
    public string CommandLineArgs { [CanBeNull] get; [CanBeNull] set; }
        
    public string ExecutablePath { [CanBeNull] get; [CanBeNull] set; }
        
    public string AutostartFlag { [CanBeNull] get; [CanBeNull] set; }
}