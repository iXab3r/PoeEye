using System;

namespace PoeShared
{
    public interface IPoeApi
    {
        IObservable<IPoeSearchResult> IssueQuery(IPoeSearchQuery query);
    }
}