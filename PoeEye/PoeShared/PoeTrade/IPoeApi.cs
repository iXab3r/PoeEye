using System;
using PoeShared.Scaffolding;

namespace PoeShared.PoeTrade
{
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    using Query;

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