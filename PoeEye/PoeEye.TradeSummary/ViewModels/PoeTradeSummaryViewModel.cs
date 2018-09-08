using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeEye.PoeTrade.ViewModels;
using PoeEye.TradeSummary.Modularity;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeEye.TradeSummary.ViewModels
{
    internal sealed class PoeTradeSummaryViewModel : OverlayViewModelBase
    {
        [NotNull] private readonly IConfigProvider<PoeTradeSummaryConfig> configProvider;

        public PoeTradeSummaryViewModel(
            [NotNull] IConfigProvider<PoeTradeSummaryConfig> configProvider,
            [NotNull] IOverlayWindowController controller,
            [NotNull] IFactory<IPoeAdvancedTradesListViewModel> listFactory,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            Guard.ArgumentNotNull(controller, nameof(controller));
            Guard.ArgumentNotNull(listFactory, nameof(listFactory));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            this.configProvider = configProvider;

            MinSize = new Size(300, double.NaN);
            MaxSize = new Size(400, double.NaN);
            Top = 200;
            Left = 200;
            Width = 300;
            Height = 800;
            SizeToContent = SizeToContent.Manual;
            IsUnlockable = true;
            Title = "Trade Summary";
            
            WhenLoaded
                .Take(1)
                .Select(x => configProvider.WhenChanged)
                .Switch()
                .Subscribe(ApplyConfig)
                .AddTo(Anchors);
        }
        
        private void ApplyConfig(PoeTradeSummaryConfig config)
        {
            base.ApplyConfig(config);
        }

        protected override void LockWindowCommandExecuted()
        {
            base.LockWindowCommandExecuted();
            
            var config = configProvider.ActualConfig;
            base.SavePropertiesToConfig(config);
            configProvider.Save(config);
        }
    }
}
