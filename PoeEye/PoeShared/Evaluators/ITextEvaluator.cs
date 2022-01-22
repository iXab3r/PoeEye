using PoeShared.Modularity;

namespace PoeShared.Evaluators;

public interface ITextEvaluator : IDisposableReactiveObject, IHasError
{
    string Text { get; set; }
    string Expression { get; set; }
    bool IsMatch { get; }
}