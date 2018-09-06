using JetBrains.Annotations;
using Newtonsoft.Json;

namespace PoeShared.Modularity
{
    public interface IConfigSerializer
    {
        void RegisterConverter([NotNull] JsonConverter converter);

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