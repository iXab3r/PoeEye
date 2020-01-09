using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PoeShared.Modularity
{
    public interface IConfigSerializer
    {
        void RegisterConverter([NotNull] JsonConverter converter);
        
        IObservable<ErrorContext> ThrownExceptions { [NotNull] get; }

        [NotNull]
        string Serialize(object data);

        [NotNull]
        T Deserialize<T>(string serializedData);

        [NotNull]
        string Compress(object data);

        [NotNull]
        T Decompress<T>(string compressedData);
    }
}