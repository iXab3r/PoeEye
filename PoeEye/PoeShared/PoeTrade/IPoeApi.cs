namespace PoeShared.PoeTrade
{
    using JetBrains.Annotations;
    using System;

    public interface IPoeApi
    {
        [NotNull] 
        IObservable<IPoeQueryResult> IssueQuery([NotNull] IPoeQuery query);
    }
}