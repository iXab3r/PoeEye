namespace PoeShared.PoeTrade
{
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    using Query;

    public interface IPoeApi
    {
        [NotNull]
        Task<IPoeQueryResult> IssueQuery([NotNull] IPoeQueryInfo query);

        [NotNull]
        Task<IPoeStaticData> RequestStaticData();
    }
}