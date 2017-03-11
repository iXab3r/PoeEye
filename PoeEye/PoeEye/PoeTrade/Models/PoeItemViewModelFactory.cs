using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeEye.PoeTrade.ViewModels;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeEye.PoeTrade.Models
{
    internal sealed class PoeItemViewModelFactory : IPoeItemViewModelFactory
    {
        [NotNull] private readonly IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory;

        public PoeItemViewModelFactory(
            [NotNull] IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory)
        {
            Guard.ArgumentNotNull(() => poeTradeViewModelFactory);
            this.poeTradeViewModelFactory = poeTradeViewModelFactory;
        }

        public IDisposableReactiveObject Create(IPoeItem item)
        {
            Guard.ArgumentNotNull(() => item);

            return poeTradeViewModelFactory.Create(item);
        }
    }
}