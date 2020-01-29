using JetBrains.Annotations;

namespace PoeShared.Modularity
{
    public interface IAppArguments
    {
        string AppName { [NotNull] get; }
        
        string AppDataDirectory { [CanBeNull] get; }
        
        bool IsDebugMode { get; }
    }
}