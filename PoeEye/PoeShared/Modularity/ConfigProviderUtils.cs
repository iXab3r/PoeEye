namespace PoeShared.Modularity;

internal static class ConfigProviderUtils
{
    public static string GetConfigName(Type configType)
    {
        return $"{configType.Namespace}.{configType.Name}";
    }

    public static string GetConfigName(IPoeEyeConfig config)
    {
        string configType;
        if (config is PoeConfigMetadata metadata)
        {
            configType = metadata.TypeName;
        }
        else
        {
            configType = GetConfigName(config.GetType());
        }

        return configType;
    }
}