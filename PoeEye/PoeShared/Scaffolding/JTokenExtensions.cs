using System.Collections.Immutable;
using Newtonsoft.Json.Linq;

namespace PoeShared.Scaffolding;

public static class JTokenExtensions
{
    public static ImmutableDictionary<string, JToken> CollectTokensByPath(this JToken token, Predicate<JToken> condition)
    {
        var tokens = new Dictionary<string, JToken>();
        EnumerateTokens(token, string.Empty, tokens, condition);
        return tokens.ToImmutableDictionary();
    }
    
    private static void EnumerateTokens(JToken token, string path, Dictionary<string, JToken> tokens, Predicate<JToken> condition)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
            {
                if (condition(token))
                {
                    tokens[path] = token;
                }
                
                foreach (var property in token.Children<JProperty>())
                {
                    var currentPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
                    EnumerateTokens(property.Value, currentPath, tokens, condition);
                }

                break;
            }
            case JTokenType.Array:
            {
                var index = 0;
                foreach (var item in token.Children())
                {
                    var currentPath = $"{path}[{index}]";
                    EnumerateTokens(item, currentPath, tokens, condition);
                    index++;
                }

                break;
            }
        }
    }
}