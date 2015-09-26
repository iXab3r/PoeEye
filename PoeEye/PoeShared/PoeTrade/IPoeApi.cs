namespace PoeShared.PoeTrade
{
    using JetBrains.Annotations;
    using System;

    using Query;

    public interface IPoeApi
    {
        [NotNull] 
        IObservable<IPoeQueryResult> IssueQuery([NotNull] IPoeQuery query);
    }
}