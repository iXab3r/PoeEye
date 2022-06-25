using System.Reflection;

namespace PoeShared.Modularity;

public sealed class ConfigMigrator
{
    private static readonly IFluentLog Log = typeof(ConfigMigrator).PrepareLogger();

    public ConfigMigrator()
    {
        
    }

    public void Migrate(IConfigProvider source, IConfigProvider target)
    {
        Log.Info($"Migrating configuration from {source} to {target}");
        if (source is ConfigProviderFromFile fromFile)
        {
            Log.Info($"Reloading single-file config");
            fromFile.Reload();
            Log.Info($"Reloaded single-file config");
        }
        
        foreach (var poeEyeConfig in source.Configs.Items)
        {
            Log.Info($"Saving configuration {poeEyeConfig.GetType()}");
            target.Save(poeEyeConfig);
            Log.Info($"Saved configuration {poeEyeConfig.GetType()}");
        }
        
        if (source is ConfigProviderFromFile fromFileConfig)
        {
            var filesToRemove = new[]
            {
                fromFileConfig.ConfigFilePath,
                Path.ChangeExtension(fromFileConfig.ConfigFilePath, "bak")
            };

            foreach (var filePath in filesToRemove)
            {
                if (File.Exists(filePath))
                {
                    Log.Info($"Removing config file {filePath}");
                    File.Delete(filePath);
                }
            }
        }
    }
}