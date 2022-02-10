using JetBrains.Annotations;
using PropertyBinder;

namespace PoeShared.Evaluators;

internal sealed class SwitchableTextEvaluator : DisposableReactiveObject, ISwitchableTextEvaluator
{
    private static readonly Binder<SwitchableTextEvaluator> Binder = new();

    static SwitchableTextEvaluator()
    {
        Binder.BindIf(x => x.Evaluator != default, x => x.Expression)
            .To(x => x.Evaluator.Expression);
        Binder.BindIf(x => x.Evaluator != default, x => x.Text)
            .To(x => x.Evaluator.Text);
        
        Binder.BindIf(x => x.Evaluator != default, x => x.Evaluator.IsMatch)
            .To(x => x.IsMatch);
        Binder.BindIf(x => x.Evaluator != default, x => x.Evaluator.Match)
            .To(x => x.Match);
        Binder.BindIf(x => x.Evaluator != default, x => x.Evaluator.Error)
            .Else(x => default)
            .To(x => x.Error);
        
        Binder.Bind(x => CreateEvaluator(x.EvaluatorType, x.IgnoreCase))
            .To(x => x.Evaluator);
    }

    public SwitchableTextEvaluator()
    {
        Binder.Attach(this).AddTo(Anchors);
    }

    public string Error { get; [UsedImplicitly] private set; }
    public string Text { get; set; }
    public string Expression { get; set; }
    public bool IsMatch { get; [UsedImplicitly] private set; }
    public string Match { get; [UsedImplicitly] private set; }
    
    public ITextEvaluator Evaluator { get; [UsedImplicitly] private set; }

    public bool IgnoreCase { get; set; }
     
    public TextEvaluatorType EvaluatorType { get; set; }

    private static ITextEvaluator CreateEvaluator(TextEvaluatorType evaluatorType, bool ignoreCase)
    {
        return evaluatorType switch
        {
            TextEvaluatorType.Lambda => new TextExpressionEvaluator(),
            TextEvaluatorType.Text => new TextEvaluator() { StringComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal },
            TextEvaluatorType.Regex => new TextRegexEvaluator() { IgnoreCase = ignoreCase },
            _ => throw new ArgumentOutOfRangeException(nameof(evaluatorType), evaluatorType, $"Unknown evaluator type")
        };
    }
}