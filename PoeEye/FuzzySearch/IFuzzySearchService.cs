namespace FuzzySearch
{
    using System.Collections.Generic;

    using JetBrains.Annotations;

    public interface IFuzzySearchService
    {
        [NotNull] 
        IEnumerable<SearchResult> Search(string needle);
    }
}