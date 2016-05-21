namespace PoeEye.Converters
{
    using System;

    using Newtonsoft.Json;

    internal sealed class ConcreteListTypeConverter<TInterface, TImplementation> : JsonConverter where TImplementation : TInterface
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (TInterface);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var res = serializer.Deserialize<TImplementation>(reader);
            return res;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}