using JetBrains.Annotations;
using PoeShared.Bindings;
using PropertyBinder;

namespace PoeShared.Evaluators;

public sealed class TextExpressionEvaluator : DisposableReactiveObject, ITextEvaluator
{
    private static readonly Binder<TextExpressionEvaluator> Binder = new();

    private readonly ExpressionWatcher watcher;

    static TextExpressionEvaluator()
    {
        Binder
            .BindIf(x => x.watcher.HasValue && x.watcher.Value is bool, x => (bool)x.watcher.Value)
            .Else(x => false)
            .To(x => x.IsMatch);
        
        Binder
            .BindIf(x => x.IsMatch, x => x.Text)
            .Else(x => default)
            .To(x => x.Match);
                
        Binder.Bind(x => x.Text)
            .To(x => x.watcher.Source);
            
        Binder.Bind(x => x.Expression)
            .To(x => x.watcher.SourceExpression);
            
        Binder.BindIf(x => x.watcher.Error != default, x => x.watcher.Error.ToString())
            .Else(x => default)
            .To(x => x.Error);
    }
            
    public TextExpressionEvaluator()
    {
        watcher = new ExpressionWatcher(typeof(bool));
        watcher.Source = this;
        Binder.Attach(this).AddTo(Anchors);
    }
            
    public string Text { get; set; }
            
    public string Expression { get; set; }
            
    public bool IsMatch { get; [UsedImplicitly] private set; }
    
    public string Match { get; [UsedImplicitly] private set; }

    public string Error { get; [UsedImplicitly] private set; }
}