namespace PoeShared.Evaluators;

public interface ISwitchableTextEvaluator : ITextEvaluator
{
    bool IgnoreCase { get; set; }
    TextEvaluatorType EvaluatorType { get; set; }
}