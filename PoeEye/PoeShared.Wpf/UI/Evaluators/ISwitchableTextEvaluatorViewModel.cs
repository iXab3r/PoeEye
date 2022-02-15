using PoeShared.Evaluators;

namespace PoeShared.UI.Evaluators;

public interface ISwitchableTextEvaluatorViewModel : ITextEvaluator
{
    bool IgnoreCase { get; set; }
    
    bool CanIgnoreCase { get; }
    
    bool TestMode { get; set; }
    
    string TestText { get; set; }
    
    TextEvaluatorType EvaluatorType { get; set; }
}