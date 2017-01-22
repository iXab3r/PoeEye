using PoeShared.Scaffolding;

namespace PoeOracle.ViewModels
{
    public abstract class OracleSuggestionViewModelBase : DisposableReactiveObject, IOracleSuggestionViewModel
    {
        public virtual void OnClick() {}
    }
}