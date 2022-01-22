using System.Text.RegularExpressions;
using JetBrains.Annotations;
using PropertyBinder;

namespace PoeShared.Evaluators;

public sealed class TextRegexEvaluator : DisposableReactiveObject, ITextEvaluator
{
    private static readonly Binder<TextRegexEvaluator> Binder = new();

    static TextRegexEvaluator()
    {
        Binder.BindIf(x => !string.IsNullOrEmpty(x.Expression), x => x.RecalculateRegex(x.Expression, x.IgnoreCase))
            .Else(x => default)
            .To(x => x.Regex);
            
        Binder.BindIf(x => x.Regex != default && x.Text != default, x => x.Regex.Match(x.Text))
            .Else(x => default)
            .To(x => x.RegexMatch);
            
        Binder.BindIf(x => x.RegexMatch != default, x => x.RegexMatch.Success)
            .Else(x => false)
            .To(x => x.IsMatch);
    }

    public TextRegexEvaluator()
    {
        Binder.Attach(this).AddTo(Anchors);
    }
        
    public Regex Regex { get; [UsedImplicitly] private set; }
        
    public Match RegexMatch { get; [UsedImplicitly] private set; }
        
    public string Text { get; set; }
        
    public bool IgnoreCase { get; set; }
            
    public string Expression { get; set; }
            
    public bool IsMatch { get; [UsedImplicitly] private set; }
        
    public string Error { get; [UsedImplicitly] private set; }

    private Regex RecalculateRegex(string text, bool ignoreCase)
    {
        Error = default;
        try
        {
            var options = RegexOptions.Compiled | RegexOptions.Singleline | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            return new Regex(text, options);
        }
        catch (Exception e)
        {
            Error = e.ToString();
            return default;
        }
    }
}