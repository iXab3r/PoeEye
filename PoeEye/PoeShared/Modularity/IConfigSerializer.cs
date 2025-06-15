using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PoeShared.Modularity;

public interface IConfigSerializer
{
    /// <summary>
    /// Specifies the maximum allowable depth when parsing or traversing hierarchical or recursive data structures,
    /// such as deeply nested JSON objects, expression trees, or configuration graphs.
    /// </summary>
    /// <remarks>
    /// This constant is used to prevent stack overflows, excessive recursion, or infinite loops caused by malformed or
    /// maliciously crafted input. It acts as a safeguard to ensure that any deserialization or processing logic respects
    /// a reasonable structural limit, even in complex data scenarios.
    ///
    /// The default maximum depth for many serializers (e.g., Newtonsoft.Json) is 64. This value is significantly higher
    /// to accommodate trusted internal use cases that are known to require greater nesting, such as behavior trees or
    /// configuration hierarchies.
    ///
    /// This constant should not be exposed for arbitrary external input without proper validation and should be adjusted
    /// only if your application is known to safely handle deeper structures.
    ///
    /// Recommended usage includes:
    /// - JSON parsing with `JsonTextReader.MaxDepth`
    /// - Stack-based recursion guards
    /// - Tree traversal or flattening logic
    /// </remarks>
    /// <example>
    /// Example: 
    /// <code>
    /// using var reader = new JsonTextReader(new StringReader(json))
    /// {
    ///     MaxDepth = MaxDepth
    /// };
    /// var token = JToken.ReadFrom(reader);
    /// </code>
    /// </example>
    public const int MaxDepth = 1024;
    
    IDisposable DisablePooling();
    
    void RegisterConverter([NotNull] JsonConverter converter);
        
    string Serialize(object data);
    
    void Serialize(object data, TextWriter textWriter);
    
    void Serialize(object data, FileInfo file);

    T Deserialize<T>(string serializedData);
    
    T Deserialize<T>(TextReader textReader);
    
    T Deserialize<T>(FileInfo file);

    T DeserializeOrDefault<T>(
        PoeConfigMetadata<T> metadata,
        Func<PoeConfigMetadata<T>, T> defaultItemFactory) where T : class;

    T[] DeserializeSingleOrList<T>(string serializedData);

    string Compress(object data);

    T Decompress<T>(string compressedData);
}