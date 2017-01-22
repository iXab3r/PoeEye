using System;
using PoeOracle.ViewModels;
using PoeShared.Scaffolding;

namespace PoeOracle.Models
{
    internal sealed class ExceptionSuggestionProvider : DisposableReactiveObject, ISuggestionProvider
    {
        public IOracleSuggestionViewModel[] Request(string query)
        {
            throw new ApplicationException();
        }
    }
}