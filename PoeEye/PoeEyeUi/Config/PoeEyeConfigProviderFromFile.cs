namespace PoeEyeUi.Config
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Converters;

    using Guards;

    using Newtonsoft.Json;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;
    using PoeShared.Scaffolding;

    using ReactiveUI;

    internal sealed class PoeEyeConfigProviderFromFile : DisposableReactiveObject, IPoeEyeConfigProvider
    {
#if DEBUG
        private static readonly string ConfigFilePath = Environment.ExpandEnvironmentVariables($@"%APPDATA%\PoeEye\configDebugMode.cfg");
#else
        private static readonly string ConfigFilePath = Environment.ExpandEnvironmentVariables($@"%APPDATA%\PoeEye\config.cfg");
#endif

        private readonly JsonSerializerSettings jsonSerializerSettings;
        private readonly IObservable<IPoeEyeConfig> actualConfigSubject;

        private Lazy<IPoeEyeConfig> poeEyeConfigLoader;

        public PoeEyeConfigProviderFromFile()
        {
            jsonSerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };

            var converters = new JsonConverter[]
            {
                new ConcreteListTypeConverter<IPoeQueryInfo, PoeQueryInfo>(),
                new ConcreteListTypeConverter<IPoeItemType, PoeItemType>(),
                new ConcreteListTypeConverter<IPoeItem, PoeItem>(),
                new ConcreteListTypeConverter<IPoeItemMod, PoeItemMod>(),
                new ConcreteListTypeConverter<IPoeLinksInfo, PoeLinksInfo>(),
                new ConcreteListTypeConverter<IPoeQueryModsGroup, PoeQueryModsGroup>(),
                new ConcreteListTypeConverter<IPoeQueryRangeModArgument, PoeQueryRangeModArgument>()
            };
            converters.ToList().ForEach(jsonSerializerSettings.Converters.Add);

            Reload();
        }

        public IPoeEyeConfig ActualConfig => poeEyeConfigLoader.Value;

        public void Reload()
        {
            poeEyeConfigLoader = new Lazy<IPoeEyeConfig>(LoadInternal);
            this.RaisePropertyChanged(nameof(ActualConfig));
        }

        public void Save(IPoeEyeConfig config)
        {
            Guard.ArgumentNotNull(() => config);

            try
            {
                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Save] Serializing config data...");
                var serializedData = JsonConvert.SerializeObject(config, jsonSerializerSettings);

                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Save] Successfully serialized config, got {serializedData.Length} chars");

                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Save] Saving config to file '{ConfigFilePath}'...");

                var directoryPath = Path.GetDirectoryName(ConfigFilePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                File.WriteAllText(ConfigFilePath, serializedData, Encoding.Unicode);

                Reload();
            }
            catch (Exception ex)
            {
                Log.Instance.Warn(
                    $"[PoeEyeConfigProviderFromFile.Save] Exception occurred, config was not save correctly",
                    ex);
            }
        }

        private IPoeEyeConfig LoadInternal()
        {
            Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Load] Loading config from file '{ConfigFilePath}'...");

            if (!File.Exists(ConfigFilePath))
            {
                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Load] File not found, fileName: '{ConfigFilePath}'");
                return new PoeEyeConfig();
            }

            PoeEyeConfig result;
            try
            {
                var fileData = File.ReadAllText(ConfigFilePath);
                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Load] Successfully read {fileData.Length} chars, deserializing...");

                result = JsonConvert.DeserializeObject<PoeEyeConfig>(fileData, jsonSerializerSettings);
                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Load] Successfully deserialized config data");
            }
            catch (Exception ex)
            {
                Log.Instance.Warn($"[PoeEyeConfigProviderFromFile.Load] Could not deserialize config data, default config will be used", ex);
                result = new PoeEyeConfig();
            }

            return result;
        }
    }
}