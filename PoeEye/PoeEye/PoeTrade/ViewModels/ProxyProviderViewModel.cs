using System;
using System.Reactive.Linq;
using Guards;
using JetBrains.Annotations;
using PoeEye.Config;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using ProxyProvider;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class ProxyProviderViewModel : DisposableReactiveObject
    {
        private readonly IProxyProvider proxyProvider;

        private int activeProxiesCount;

        private bool isProxified;

        private int totalProxiesCount;

        public ProxyProviderViewModel(
            [NotNull] IProxyProvider proxyProvider,
            [NotNull] IConfigProvider<PoeEyeMainConfig> configProvider)
        {
            Guard.ArgumentNotNull(proxyProvider, nameof(proxyProvider));
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));

            this.proxyProvider = proxyProvider;

            configProvider
                .WhenChanged
                .Select(x => x.ProxyRecheckTimeout)
                .Select(x => Observable.Timer(DateTimeOffset.Now, x))
                .Switch()
                .Subscribe(Refresh)
                .AddTo(Anchors);
        }

        public int ActiveProxiesCount
        {
            get => activeProxiesCount;
            set => this.RaiseAndSetIfChanged(ref activeProxiesCount, value);
        }

        public int TotalProxiesCount
        {
            get => totalProxiesCount;
            set => this.RaiseAndSetIfChanged(ref totalProxiesCount, value);
        }

        public bool IsProxified
        {
            get => isProxified;
            set => this.RaiseAndSetIfChanged(ref isProxified, value);
        }

        private void Refresh()
        {
            ActiveProxiesCount = proxyProvider.ActiveProxiesCount;
            TotalProxiesCount = proxyProvider.TotalProxiesCount;

            IsProxified = activeProxiesCount > 0;
        }
    }
}