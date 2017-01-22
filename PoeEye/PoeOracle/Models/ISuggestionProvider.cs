using JetBrains.Annotations;
using PoeOracle.ViewModels;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeOracle.Models
{
    internal interface ISuggestionProvider : IDisposableReactiveObject
    {
        [NotNull]
        IOracleSuggestionViewModel[] Request([CanBeNull] string query);
    }
}