using System.IO;
using log4net;

namespace PoeShared.Modularity
{
    public sealed class UseDefaultIfFailureConfigProviderStrategy : IConfigProviderStrategy
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UseDefaultIfFailureConfigProviderStrategy));

        public void HandleConfigSave(FileInfo configFile)
        {
        }

        public bool TryHandleConfigLoadException(FileInfo configFile, out ConfigProviderFromFile.PoeEyeCombinedConfig replacementConfig)
        {
            Log.Debug($"Using empty config due to failure in {configFile}");
            replacementConfig = new ConfigProviderFromFile.PoeEyeCombinedConfig();
            return true;
        }
    }
}