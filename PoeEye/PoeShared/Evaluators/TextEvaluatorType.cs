using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PoeShared.Evaluators;

/// <summary>
/// Enumerates different types of text evaluators that can be used in a text evaluation process.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum TextEvaluatorType
{
    /// <summary>
    /// Represents a regular expression evaluator.
    /// </summary>
    [EnumMember(Value = nameof(Regex))]
    Regex,

    /// <summary>
    /// Represents a Lambda expression evaluator.
    /// </summary>
    [EnumMember(Value = nameof(Lambda))]
    Lambda,        

    /// <summary>
    /// Represents a basic text comparison evaluator.
    /// </summary>
    [EnumMember(Value = nameof(Text))]
    Text,
}
