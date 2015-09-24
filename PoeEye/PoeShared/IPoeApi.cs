namespace PoeShared
{
    using System;

    public interface IPoeApi
    {
        IObservable<IPoeSearchResult> IssueQuery(IPoeSearchQuery query);

        string[] ExtractCurrenciesList();
    }
}