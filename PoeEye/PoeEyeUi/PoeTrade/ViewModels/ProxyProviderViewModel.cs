namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Reactive.Linq;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.Scaffolding;

    using ProxyProvider;

    using ReactiveUI;

    internal sealed class ProxyProviderViewModel : DisposableReactiveObject
    {
        private readonly TimeSpan ProxiesRecheckPeriod = TimeSpan.FromSeconds(10);

        private readonly IProxyProvider proxyProvider;

        private int activeProxiesCount;

        private bool isProxified;

        private int totalProxiesCount;

        public ProxyProviderViewModel([NotNull] IProxyProvider proxyProvider)
        {
            Guard.ArgumentNotNull(() => proxyProvider);

            this.proxyProvider = proxyProvider;

            Observable
                .Timer(DateTimeOffset.Now, ProxiesRecheckPeriod)
                .Subscribe(Refresh)
                .AddTo(Anchors);
        }

        public int ActiveProxiesCount
        {
            get { return activeProxiesCount; }
            set { this.RaiseAndSetIfChanged(ref activeProxiesCount, value); }
        }

        public int TotalProxiesCount
        {
            get { return totalProxiesCount; }
            set { this.RaiseAndSetIfChanged(ref totalProxiesCount, value); }
        }

        public bool IsProxified
        {
            get { return isProxified; }
            set { this.RaiseAndSetIfChanged(ref isProxified, value); }
        }

        private void Refresh()
        {
            ActiveProxiesCount = proxyProvider.ActiveProxiesCount;
            TotalProxiesCount = proxyProvider.TotalProxiesCount;

            IsProxified = activeProxiesCount > 0;
        }
    }
}