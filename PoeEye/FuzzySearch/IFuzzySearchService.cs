using System.Collections.Generic;
using JetBrains.Annotations;

namespace FuzzySearch
{
    public interface IFuzzySearchService
    {
        [NotNull]
        IEnumerable<SearchResult> Search(string needle);
    }
}