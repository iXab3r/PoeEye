using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PoeShared.UI.Evaluators;

[JsonConverter(typeof(StringEnumConverter))]
public enum TextEvaluatorType
{
    [EnumMember(Value = nameof(Regex))]
    Regex,
    [EnumMember(Value = nameof(Lambda))]
    Lambda,        
    [EnumMember(Value = nameof(Text))]
    Text,
}