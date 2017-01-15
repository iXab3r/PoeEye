using PoeShared;

namespace PoeBud.Config
{
    using System;
    using System.IO;
    using System.Reactive;
    using System.Reactive.Subjects;
    using System.Text;

    using Guards;

    using Newtonsoft.Json;

    internal sealed class PoeBudConfigProviderFromFile : IPoeBudConfigProvider<IPoeBudConfig>
    {
#if DEBUG
        private static readonly string ConfigFilePath = Environment.ExpandEnvironmentVariables($@"%APPDATA%\PoeBud\configDebugMode.cfg");
#else
        private static readonly string ConfigFilePath = Environment.ExpandEnvironmentVariables($@"%APPDATA%\PoeBud\config.cfg");
#endif

        private readonly JsonSerializerSettings jsonSerializerSettings;
        private readonly ISubject<Unit> configUpdatedSubject;

        private Lazy<IPoeBudConfig> activeConfig; 

        public PoeBudConfigProviderFromFile()
        {
            jsonSerializerSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
            };

            configUpdatedSubject = new BehaviorSubject<Unit>(Unit.Default);

            configUpdatedSubject
                .Subscribe(x => activeConfig = new Lazy<IPoeBudConfig>(LoadInternal));
        }

        public void Save(IPoeBudConfig config)
        {
            Guard.ArgumentNotNull(() => config);

            try
            {
                Log.Instance.Debug($"[PoeBudConfigProviderFromFile.Save] Serializing config data...");
                var serializedData = JsonConvert.SerializeObject(config, jsonSerializerSettings);

                Log.Instance.Debug($"[PoeBudConfigProviderFromFile.Save] Successfully serialized config, got {serializedData.Length} chars");

                Log.Instance.Debug($"[PoeBudConfigProviderFromFile.Save] Saving config to file '{ConfigFilePath}'...");

                var directoryPath = Path.GetDirectoryName(ConfigFilePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                File.WriteAllText(ConfigFilePath, serializedData, Encoding.Unicode);
            }
            catch (Exception ex)
            {
                Log.Instance.Warn(
                    $"[PoeBudConfigProviderFromFile.Save] Exception occurred, config was not save correctly",
                    ex);
            }

            configUpdatedSubject.OnNext(Unit.Default);
        }

        public IObservable<Unit> ConfigUpdated => configUpdatedSubject;

        public IPoeBudConfig Load()
        {
            return activeConfig.Value;
        }

        private IPoeBudConfig LoadInternal()
        {
            Log.Instance.Debug($"[PoeBudConfigProviderFromFile.Load] Loading config from file '{ConfigFilePath}'...");

            if (!File.Exists(ConfigFilePath))
            {
                Log.Instance.Debug($"[PoeBudConfigProviderFromFile.Load] File not found, fileName: '{ConfigFilePath}'");
                var newConfig = new PoeBudConfig();
                Save(newConfig);
                return newConfig;
            }

            IPoeBudConfig result;
            try
            {
                var fileData = File.ReadAllText(ConfigFilePath);
                Log.Instance.Debug($"[PoeBudConfigProviderFromFile.Load] Successfully read {fileData.Length} chars, deserializing...:{fileData}");

                result = JsonConvert.DeserializeObject<PoeBudConfig>(fileData, jsonSerializerSettings);
                Log.Instance.Debug($"[PoeBudConfigProviderFromFile.Load] Successfully deserialized config data");
            }
            catch (Exception ex)
            {
                Log.Instance.Warn($"[PoeBudConfigProviderFromFile.Load] Could not deserialize config data, default config will be used", ex);
                result = new PoeBudConfig();
            }

            return result;
        }
    }
}