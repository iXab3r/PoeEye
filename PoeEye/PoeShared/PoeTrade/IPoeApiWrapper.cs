using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.PoeTrade.Query;

namespace PoeShared.PoeTrade
{
    public interface IPoeApiWrapper
    {
        [NotNull]
        IPoeStaticData StaticData { [NotNull] get; }

        [NotNull]
        Task<IPoeQueryResult> IssueQuery([NotNull] IPoeQueryInfo query);

        bool IsBusy { get; }

        string Name { [NotNull] get; }
    }
}