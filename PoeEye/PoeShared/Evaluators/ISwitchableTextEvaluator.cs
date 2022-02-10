namespace PoeShared.Evaluators;

public interface ISwitchableTextEvaluator : ITextEvaluator
{
    bool IgnoreCase { get; set; }
    
    bool CanIgnoreCase { get; }
    
    TextEvaluatorType EvaluatorType { get; set; }
}