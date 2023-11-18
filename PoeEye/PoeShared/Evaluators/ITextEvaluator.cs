using PoeShared.Modularity;

namespace PoeShared.Evaluators;

public interface ITextEvaluator : IDisposableReactiveObject, IHasError
{
    /// <summary>
    /// The text to be evaluated or matched against the expression.
    /// </summary>
    string Text { get; set; }

    /// <summary>
    /// The expression used for matching, which can be a regular expression, text search pattern, or a C# expression depending on exact evaluator type
    /// </summary>
    string Expression { get; set; }

    /// <summary>
    /// Indicates whether the text matches the expression.
    /// </summary>
    bool IsMatch { get; }

    /// <summary>
    /// The part of the text that matches the expression, if any.
    /// </summary>
    string Match { get; }
}
