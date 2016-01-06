namespace PoeShared.PoeTrade
{
    using JetBrains.Annotations;
    using System;
    using System.Threading.Tasks;

    using Query;

    public interface IPoeApi
    {
        [NotNull] 
        Task<IPoeQueryResult> IssueQuery([NotNull] IPoeQuery query);

        [NotNull]
        Task<IPoeQueryResult> GetStaticData();
    }
}