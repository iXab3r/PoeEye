using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;

namespace PoeShared.PoeTrade
{
    public interface IPoeApi : IDisposableReactiveObject
    {
        Guid Id { get; }

        string Name { [NotNull] get; }

        bool IsAvailable { get; }

        [NotNull]
        Task<IPoeQueryResult> IssueQuery([NotNull] IPoeQueryInfo query);

        [NotNull]
        Task<IPoeStaticData> RequestStaticData();

        void DisposeQuery([NotNull] IPoeQueryInfo query);
    }
}