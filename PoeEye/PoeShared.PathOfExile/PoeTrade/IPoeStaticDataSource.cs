using JetBrains.Annotations;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;

namespace PoeShared.PoeTrade
{
    public interface IPoeStaticDataSource : IDisposableReactiveObject
    {
        [NotNull]
        IPoeStaticData StaticData { [NotNull] get; }
    }
}