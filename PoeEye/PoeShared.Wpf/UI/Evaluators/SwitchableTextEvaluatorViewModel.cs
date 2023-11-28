using System;
using JetBrains.Annotations;
using PoeShared.Evaluators;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.UI.Evaluators;

internal sealed class SwitchableTextEvaluatorViewModel : DisposableReactiveObject, ISwitchableTextEvaluatorViewModel
{
    private static readonly Binder<SwitchableTextEvaluatorViewModel> Binder = new();

    static SwitchableTextEvaluatorViewModel()
    {
        Binder.BindIf(x => x.Evaluator != default, x => x.Expression)
            .To(x => x.Evaluator.Expression);
        
        Binder.BindIf(x => x.TestMode == false, x => x.Text)
            .To(x => x.TestText);
        
        Binder.BindIf(x => x.Evaluator != default && x.TestMode == false, x => x.Text)
            .ElseIf(x => x.Evaluator != default && x.TestMode == true, x => x.TestText)
            .To(x => x.Evaluator.Text);

        Binder.BindIf(x => x.Evaluator != default, x => x.Evaluator.IsMatch)
            .To(x => x.IsMatch);
        Binder.BindIf(x => x.Evaluator != default, x => x.Evaluator.Match)
            .To(x => x.Match);
        Binder.BindIf(x => x.Evaluator != default, x => x.Evaluator.LastError)
            .Else(x => default)
            .To(x => x.LastError);

        Binder.Bind(x => CreateEvaluator(x.EvaluatorType, x.IgnoreCase))
            .To(x => x.Evaluator);

        Binder.Bind(x => x.EvaluatorType == TextEvaluatorType.Regex || x.EvaluatorType == TextEvaluatorType.Text)
            .To(x => x.CanIgnoreCase);
    }

    public SwitchableTextEvaluatorViewModel()
    {
        Binder.Attach(this).AddTo(Anchors);
    }

    public ITextEvaluator Evaluator { get; [UsedImplicitly] private set; }

    public ErrorInfo? LastError { get; [UsedImplicitly] private set; }
    public string Text { get; set; }
    public string Expression { get; set; }
    public bool IsMatch { get; [UsedImplicitly] private set; }
    public string Match { get; [UsedImplicitly] private set; }

    public bool IgnoreCase { get; set; }

    public bool CanIgnoreCase { get; [UsedImplicitly] private set; }
    
    public bool TestMode { get; set; }
    
    public string TestText { get; [UsedImplicitly] set; }

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