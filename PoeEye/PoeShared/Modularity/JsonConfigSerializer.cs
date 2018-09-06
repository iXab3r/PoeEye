using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Guards;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Modularity
{
    internal class JsonConfigSerializer : IConfigSerializer
    {
        private JsonSerializerSettings jsonSerializerSettings;
        private readonly IReactiveList<JsonConverter> converters = new ReactiveList<JsonConverter>();
        
        public JsonConfigSerializer()
        {
            converters.Changed
                .ToUnit()
                .StartWith(Unit.Default)
                .Subscribe(ReinitializeSerializerSettings);
        }

        public void RegisterConverter(JsonConverter converter)
        {
            Guard.ArgumentNotNull(converter, nameof(converter));

            converters.Add(converter);
        }

        public string Serialize(object data)
        {
            return JsonConvert.SerializeObject(data, jsonSerializerSettings);
        }

        public T Deserialize<T>(string serializedData)
        {
            return JsonConvert.DeserializeObject<T>(serializedData, jsonSerializerSettings);
        }

        public string Compress(object data)
        {
            throw new System.NotImplementedException();
        }

        public T Decompress<T>(string compressedData)
        {
            throw new System.NotImplementedException();
        }

        private void ReinitializeSerializerSettings()
        {
            jsonSerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple,
                Error = HandleSerializerError
            };

            converters.ToList().ForEach(jsonSerializerSettings.Converters.Add);
        }
        
        private void HandleSerializerError(object sender, ErrorEventArgs args)
        {
            if (sender == null || args == null)
            {
                return;
            }
            //FIXME Serializer errors should be treated appropriately, e.g. load value from default config on error
            Log.Instance.Warn($"[PoeEyeConfigProviderFromFile.SerializerError] Suppresing serializer error ! Path: {args.ErrorContext.Path}, Member: {args.ErrorContext.Member}, Handled: {args.ErrorContext.Handled}", args.ErrorContext.Error);
            args.ErrorContext.Handled = true;
        }
    }
}