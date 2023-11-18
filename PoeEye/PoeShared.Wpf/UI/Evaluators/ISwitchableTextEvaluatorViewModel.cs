using PoeShared.Evaluators;

namespace PoeShared.UI.Evaluators;

/// <summary>
/// Represents a view model for a text evaluator that can switch between different evaluator types.
/// It also provides functionality for testing expressions against a custom text.
/// </summary>
public interface ISwitchableTextEvaluatorViewModel : ITextEvaluator
{
    /// <summary>
    /// Indicates whether the text evaluation should ignore case sensitivity.
    /// </summary>
    bool IgnoreCase { get; set; }

    /// <summary>
    /// Gets a value indicating whether the evaluator supports case-insensitive matching.
    /// </summary>
    bool CanIgnoreCase { get; }

    /// <summary>
    /// Enables or disables test mode, allowing the evaluator to use TestText for validation instead of the actual text.
    /// </summary>
    bool TestMode { get; set; }

    /// <summary>
    /// Text used for testing the evaluator expression when TestMode is active.
    /// </summary>
    string TestText { get; set; }

    /// <summary>
    /// Specifies the type of text evaluator (e.g., Regex, Lambda, Text) used in the view model.
    /// </summary>
    TextEvaluatorType EvaluatorType { get; set; }
}
