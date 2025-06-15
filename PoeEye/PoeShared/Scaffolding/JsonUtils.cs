using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoeShared.Modularity;

namespace PoeShared.Scaffolding;

public static class JsonUtils
{
    /// <summary>
    /// Safely parses a JSON string into a <see cref="JToken"/> while enforcing a maximum depth limit to protect against
    /// excessively nested or potentially malicious JSON structures.
    /// </summary>
    /// <param name="json">The JSON string to parse into a <see cref="JToken"/>.</param>
    /// <returns>
    /// A <see cref="JToken"/> representing the root of the parsed JSON structure. The result may be a <see cref="JObject"/>,
    /// <see cref="JArray"/>, or any other supported token type depending on the input.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="json"/> string is null.</exception>
    /// <exception cref="JsonReaderException">
    /// Thrown if the JSON is malformed or exceeds the configured maximum depth (<see cref="IConfigSerializer.MaxDepth"/>).
    /// </exception>
    /// <remarks>
    /// This method uses <see cref="JsonTextReader"/> internally with a strict maximum depth defined by
    /// <see cref="IConfigSerializer.MaxDepth"/> to prevent stack overflows or memory exhaustion from deeply nested input.
    /// It's particularly useful when consuming external or user-supplied data that may be untrusted.
    ///
    /// Usage of this method provides enhanced safety compared to directly calling <see cref="JToken.Parse(string)"/>,
    /// which does not impose a depth limit by default.
    ///
    /// Example:
    /// <code>
    /// var token = Utils.ParseJToken(jsonString);
    /// var config = token["settings"]?["theme"]?.Value&lt;string&gt;();
    /// </code>
    /// </remarks>
    public static JToken ParseJToken(string json)
    {
        if (json == null) throw new ArgumentNullException(nameof(json));

        using var stringReader = new StringReader(json);
        using var jsonReader = new JsonTextReader(stringReader);
        jsonReader.MaxDepth = IConfigSerializer.MaxDepth;

        return JToken.ReadFrom(jsonReader);
    }
    
    /// <summary>
    /// Safely parses a JSON string into a <see cref="JObject"/>, enforcing a maximum depth limit to mitigate risks
    /// associated with deeply nested or malicious JSON payloads.
    /// </summary>
    /// <param name="json">The JSON string to parse into a <see cref="JObject"/>.</param>
    /// <returns>
    /// A <see cref="JObject"/> representing the root of the parsed JSON object. 
    /// If the input is not a valid JSON object, an exception will be thrown.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="json"/> parameter is null.</exception>
    /// <exception cref="JsonReaderException">
    /// Thrown if the input JSON is malformed, not an object, or exceeds the maximum allowed depth
    /// as defined by <see cref="IConfigSerializer.MaxDepth"/>.
    /// </exception>
    /// <remarks>
    /// This method utilizes a <see cref="JsonTextReader"/> with an explicitly defined maximum depth,
    /// providing robust protection against excessively nested structures that could otherwise lead to
    /// stack overflow exceptions or performance degradation.
    ///
    /// This is particularly important when parsing user-generated or externally sourced JSON content,
    /// where input structure cannot be guaranteed.
    ///
    /// Example usage:
    /// <code>
    /// var obj = Utils.ParseJObject(jsonString);
    /// var name = obj["user"]?["name"]?.Value&lt;string&gt;();
    /// </code>
    /// </remarks>
    public static JObject ParseJObject(string json)
    {
        if (json == null) throw new ArgumentNullException(nameof(json));

        using var stringReader = new StringReader(json);
        using var jsonReader = new JsonTextReader(stringReader);
        jsonReader.MaxDepth = IConfigSerializer.MaxDepth;

        return JObject.Load(jsonReader);
    }
}