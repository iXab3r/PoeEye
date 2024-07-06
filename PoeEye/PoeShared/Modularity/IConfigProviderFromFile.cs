using JetBrains.Annotations;

namespace PoeShared.Modularity;

public interface IConfigProviderFromFile : IConfigProvider
{
    string ConfigFilePath { [NotNull] get; }

    void SaveToFile(FileInfo file);
    
    void SaveToFile(FileInfo file, IReadOnlyList<IPoeEyeConfig> configs);

    void Reload();

    IDisposable RegisterStrategy(IConfigProviderStrategy strategy);
}