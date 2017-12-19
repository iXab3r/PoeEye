using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;

namespace PoeShared.PoeTrade
{
    public interface IPoeApiWrapper : IPoeStaticDataSource, IDisposableReactiveObject
    {
        [NotNull]
        Task<IPoeQueryResult> IssueQuery([NotNull] IPoeQueryInfo query);

        void DisposeQuery([NotNull] IPoeQueryInfo query);

        bool IsBusy { get; }

        bool IsAvailable { get; }
        
        string Name { [NotNull] get; }

        Guid Id { get; }
    }
}