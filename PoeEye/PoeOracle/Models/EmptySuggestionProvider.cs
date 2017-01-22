using System.Threading;
using PoeOracle.ViewModels;
using PoeShared.Scaffolding;

namespace PoeOracle.Models
{
    internal sealed class EmptySuggestionProvider : DisposableReactiveObject, ISuggestionProvider
    {
        public IOracleSuggestionViewModel[] Request(string query)
        {
            Thread.Sleep(1000);
            return new IOracleSuggestionViewModel[0];
        }
    }
}