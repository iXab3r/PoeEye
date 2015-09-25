namespace PoeShared
{
    using System;

    using Query;

    public interface IPoeApi
    {
        IObservable<IPoeQueryResult> IssueQuery(IPoeQuery query);
    }
}