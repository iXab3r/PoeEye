using System.Reactive;
using System.Reactive.Subjects;
using System.Text;
using DynamicData;
using PoeShared.Services;

namespace PoeShared.Modularity;

public sealed class ConfigProviderFromMultipleFiles : DisposableReactiveObject, IConfigProvider
{
    private static readonly IFluentLog Log = typeof(ConfigProviderFromMultipleFiles).PrepareLogger();
    private readonly IConfigSerializer configSerializer;

    private readonly SourceCache<IPoeEyeConfig, string> loadedConfigsByType = new(x => $"{x.GetType().Namespace}.{x.GetType().Name}");
    private readonly ISubject<Unit> configHasChanged = new Subject<Unit>();
    private readonly DirectoryInfo configDirectory;
    private readonly NamedLock fileLock = new("MultiFileConfigLock");

    public ConfigProviderFromMultipleFiles(
        IConfigSerializer configSerializer,
        IAppArguments appArguments)
    {
        Log.Info("Initializing multi-file config provider");

        this.configSerializer = configSerializer;

        var candidates = new[]
            {
                Path.Combine(appArguments.AppDomainDirectory, "config"),
                appArguments.SharedAppDataDirectory
            }
            .Select(x => new DirectoryInfo(x))
            .ToArray();
        Log.Debug(() => $"Configuration matrix, directories:\n\t{candidates.SelectSafe(x => new { x.FullName, x.Exists }).DumpToString()}");
        var existingDirectory = candidates.FirstOrDefault(x => x.Exists);
        if (existingDirectory != null)
        {
            configDirectory = existingDirectory;
            Log.Info($"Using existing configuration directory {configDirectory}");
        }
        else
        {
            configDirectory = candidates.Last();
            Log.Info($"Configuration directory not found, using {configDirectory}");
        }

        if (!configDirectory.Exists)
        {
            Log.Info($"Creating configuration directory: {configDirectory.FullName}");
            Directory.CreateDirectory(configDirectory.FullName);
        }

        configSerializer.ThrownExceptions
            .Subscribe(
                errorContext =>
                {
                    //FIXME Serializer errors should be treated appropriately, e.g. load value from default config on error
                    Log.Warn(
                        $"[PoeEyeConfigProviderFromFile.SerializerError] Suppressing serializer error ! Path: {errorContext.Path}, Member: {errorContext.Member}, Handled: {errorContext.Handled}",
                        errorContext.Error);
                    errorContext.Handled = true;
                })
            .AddTo(Anchors);
    }

    public IObservable<Unit> ConfigHasChanged => configHasChanged;

    public void Save()
    {
        Log.Info(() => $"Saving all configs, count: {loadedConfigsByType.Count}");
        foreach (var config in loadedConfigsByType.Items)
        {
            Log.Info(() => $"Saving config of type {config.GetType()}");
            SaveInternal(config);
            Log.Info(() => $"Saved config of type {config.GetType()}");
        }
    }

    public void Save<TConfig>(TConfig config) where TConfig : IPoeEyeConfig, new()
    {
        using var @lock = fileLock.Enter();
        
        loadedConfigsByType.AddOrUpdate(config);
        SaveInternal(config);
    }

    public TConfig GetActualConfig<TConfig>() where TConfig : IPoeEyeConfig, new()
    {
        using var @lock = fileLock.Enter();

        var configName = GetConfigName(typeof(TConfig));
        if (loadedConfigsByType.TryGetValue(configName, out var existingConfig))
        {
            return (TConfig) existingConfig;
        }
        
        Log.Debug($"Config of type {typeof(TConfig)} is not loaded, loading it from storage");
        var config = LoadInternal<TConfig>();
        loadedConfigsByType.AddOrUpdate(config);

        if (config is not PoeConfigMetadata metadata)
        {
            return config;
        }

        Log.Debug(() => $"Trying to re-serialize metadata type {metadata.TypeName} (v{metadata.Version}) {metadata.AssemblyName}...");
        var serialized = configSerializer.Serialize(metadata);
        if (string.IsNullOrEmpty(serialized))
        {
            throw new ApplicationException($"Something went wrong when re-serializing metadata: {metadata}\n{metadata.ConfigValue}");
        }

        var deserialized = configSerializer.Deserialize<TConfig>(serialized);
        return deserialized;
    }
    
    private void SaveInternal(IPoeEyeConfig config)
    {
        var configFilePath = GetConfigFilePath(config.GetType());
        try
        {
            Log.Debug(() => $"Saving config to file '{configFilePath}'");
            Log.Debug(() => $"Serializing config data...");
            var serializedData = configSerializer.Serialize(config);

            Log.Debug(() => $"Successfully serialized config, got {serializedData.Length} chars");

            var directoryPath = Path.GetDirectoryName(configFilePath);
            if (directoryPath != null && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var temporaryConfigPath = Path.ChangeExtension(configFilePath, ".new");
            var backupConfigPath = Path.ChangeExtension(configFilePath, ".bak");
            if (string.IsNullOrEmpty(temporaryConfigPath) || string.IsNullOrEmpty(backupConfigPath))
            {
                throw new ApplicationException($"Failed to prepare path for a temporary config file, file path: {configFilePath}");
            }
            var configFile = new FileInfo(configFilePath);
            var temporaryFile = new FileInfo(temporaryConfigPath);
            var backupFile = new FileInfo(backupConfigPath);

            Log.Debug(() => $"Preparing temporary file '{temporaryFile}'...");
            if (temporaryFile.Exists)
            {
                Log.Debug(() => $"Removing previous temporary file '{temporaryFile}'...");
                temporaryFile.Delete();
            }

            Log.Debug(() => $"Writing configuration({serializedData.Length}) to temporary file '{temporaryFile}'...");
            File.WriteAllText(temporaryConfigPath, serializedData, Encoding.Unicode);
            temporaryFile.Refresh();
            Log.Debug(() => $"Flushing configuration '{temporaryConfigPath}' => '{configFilePath}'");

            if (backupFile.Exists)
            {
                Log.Debug(() => $"Removing previous backup file {backupFile}");
                backupFile.Delete();
            }

            if (configFile.Exists)
            {
                Log.Debug(() => $"Moving previous config to backup {configFile.FullName} => {backupFile.FullName}");
                configFile.MoveTo(backupConfigPath);   
            }
                
            Log.Debug(() => $"Moving temporary config to default {temporaryFile.FullName} => {configFile.FullName}");
            temporaryFile.MoveTo(configFilePath);
        }
        catch (Exception ex)
        {
            Log.Warn($"Exception occurred, config was not saved correctly", ex);
        }
    }

    private string GetConfigFilePath(Type configType)
    {
        var configFilePath = Path.Combine(configDirectory.FullName, $"{GetConfigName(configType)}.cfg");
        return configFilePath;
    }

    private string GetConfigName(Type configType)
    {
        return $"{configType.Namespace}.{configType.Name}";
    }
    
    private TConfig LoadInternal<TConfig>() where TConfig : new()
    {
        var configFilePath = GetConfigFilePath(typeof(TConfig));
        Log.Debug(() => $"Loading config from file '{configFilePath}'");
        if (!File.Exists(configFilePath))
        {
            Log.Debug(() => $"File not found, fileName: '{configFilePath}'");
            return new TConfig();
        }

        TConfig result = default;
        try
        {
            var fileData = File.ReadAllText(configFilePath);
            Log.Debug(() => $"Successfully read {fileData.Length} chars, deserializing...");

            result = configSerializer.Deserialize<TConfig>(fileData);
            Log.Debug(() => $"Successfully deserialized config data");
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not deserialize config data", ex);
        }

        if (result == null)
        {
            throw new ApplicationException($"Could not load configuration from {configFilePath}");
        }

        return result;
    }
}