namespace PoeShared.Modularity;

public sealed class UseDefaultIfFailureConfigProviderStrategy : IConfigProviderStrategy
{
    private static readonly IFluentLog Log = typeof(UseDefaultIfFailureConfigProviderStrategy).PrepareLogger();

    public void HandleConfigSave(FileInfo configFile)
    {
    }

    public bool TryHandleConfigLoadException(FileInfo configFile, out ConfigProviderFromFile.PoeEyeCombinedConfig replacementConfig)
    {
        Log.Debug(() => $"Using empty config due to failure in {configFile}");
        replacementConfig = new ConfigProviderFromFile.PoeEyeCombinedConfig();
        return true;
    }
}