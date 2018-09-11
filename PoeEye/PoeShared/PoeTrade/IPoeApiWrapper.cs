using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;

namespace PoeShared.PoeTrade
{
    public interface IPoeApiWrapper : IPoeStaticDataSource, IDisposableReactiveObject
    {
        bool IsBusy { get; }

        bool IsAvailable { get; }

        string Name { [NotNull] get; }
        
        string Error { [CanBeNull] get; }

        Guid Id { get; }

        [NotNull]
        Task<IPoeQueryResult> IssueQuery([NotNull] IPoeQueryInfo query);

        void DisposeQuery([NotNull] IPoeQueryInfo query);
    }
}