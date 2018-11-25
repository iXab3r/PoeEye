using JetBrains.Annotations;
using PoeShared.Common;
using PoeShared.Scaffolding;

namespace PoeShared.PoeTrade
{
    public interface IPoeItemViewModelFactory
    {
        [NotNull]
        IDisposableReactiveObject Create([NotNull] IPoeItem item);
    }
}