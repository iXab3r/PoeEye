namespace PoeShared.PoeTrade
{
    using System;

    public interface IPoeApi
    {
        IObservable<IPoeQueryResult> IssueQuery(IPoeQuery query);
    }
}