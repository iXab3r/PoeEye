using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PoeShared.Modularity;

public interface IConfigSerializer
{
    void RegisterConverter([NotNull] JsonConverter converter);
        
    string Serialize(object data);
    
    void Serialize(object data, TextWriter textWriter);
    
    void Serialize(object data, FileInfo file);

    T Deserialize<T>(string serializedData);
    
    T Deserialize<T>(TextReader textReader);
    
    T Deserialize<T>(FileInfo file);

    T DeserializeOrDefault<T>(
        PoeConfigMetadata<T> metadata,
        Func<PoeConfigMetadata<T>, T> defaultItemFactory) where T : IPoeEyeConfig;

    T[] DeserializeSingleOrList<T>(string serializedData);

    string Compress(object data);

    T Decompress<T>(string compressedData);
}