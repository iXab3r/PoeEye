using JetBrains.Annotations;
using PoeOracle.ViewModels;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeOracle.Models
{
    internal interface ISuggestionsDataSource : IDisposableReactiveObject
    {
        IReactiveList<IOracleSuggestionViewModel> Items { [NotNull] get; }

        bool IsBusy { get; }

        string Query { [CanBeNull] get; [CanBeNull] set; }
    }
}