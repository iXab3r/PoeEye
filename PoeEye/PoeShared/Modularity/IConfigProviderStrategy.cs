using System;
using System.IO;

namespace PoeShared.Modularity;

public interface IConfigProviderStrategy
{
    void HandleConfigSave(FileInfo configFile);
        
    bool TryHandleConfigLoadException(FileInfo configFile, out ConfigProviderFromFile.PoeEyeCombinedConfig replacementConfig);
}