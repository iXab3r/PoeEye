using JetBrains.Annotations;
using PropertyBinder;

namespace PoeShared.Evaluators;

public sealed class TextEvaluator : DisposableReactiveObject, ITextEvaluator
{
    private static readonly Binder<TextEvaluator> Binder = new();

    static TextEvaluator()
    {
        Binder
            .Bind(x => x.Text != null && x.Expression != null && string.Compare(x.Text, x.Expression, x.StringComparison) == 0)
            .To(x => x.IsMatch);
        
        Binder.BindIf(x => x.IsMatch, x => x.Text)
            .Else(x => default)
            .To(x => x.Match);
    }

    public TextEvaluator()
    {
        Binder.Attach(this).AddTo(Anchors);
    }

    public StringComparison StringComparison { get; set; }

    public string Error { get; [UsedImplicitly] private set; }
    public string Text { get; set; }
    public string Expression { get; set; }
    public bool IsMatch { get; [UsedImplicitly] private set; }
    public string Match { get; [UsedImplicitly] private set; }
}