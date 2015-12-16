namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Reactive.Linq;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.Utilities;

    using ProxyProvider;

    using ReactiveUI;

    internal sealed class ProxyProviderViewModel : DisposableReactiveObject
    {
        private readonly TimeSpan ProxiesRecheckPeriod = TimeSpan.FromSeconds(10);

        private readonly IProxyProvider proxyProvider;

        public ProxyProviderViewModel([NotNull] IProxyProvider proxyProvider)
        {
            Guard.ArgumentNotNull(() => proxyProvider);

            this.proxyProvider = proxyProvider;

            Observable
                .Timer(DateTimeOffset.Now, ProxiesRecheckPeriod)
                .Subscribe(Refresh)
                .AddTo(Anchors);
        }

        private int activeProxiesCount;

        public int ActiveProxiesCount
        {
            get { return activeProxiesCount; }
            set { this.RaiseAndSetIfChanged(ref activeProxiesCount, value); }
        }

        private int totalProxiesCount;

        public int TotalProxiesCount
        {
            get { return totalProxiesCount; }
            set { this.RaiseAndSetIfChanged(ref totalProxiesCount, value); }
        }

        private bool isProxified;

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