namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Reactive.Linq;

    using Config;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.Scaffolding;

    using ReactiveUI;

    internal sealed class RecheckPeriodViewModel : DisposableReactiveObject, IRecheckPeriodViewModel
    {
        private bool isAutoRecheckEnabled;

        private TimeSpan maxValue = TimeSpan.FromMinutes(30);

        private TimeSpan minValue = TimeSpan.FromMinutes(5);

        private TimeSpan period = TimeSpan.FromMinutes(5);

        public RecheckPeriodViewModel([NotNull] IPoeEyeConfigProvider configProvider)
        {
            Guard.ArgumentNotNull(() => configProvider);

            this.WhenAnyValue(x => x.Period)
                .Where(x => x == TimeSpan.Zero)
                .Subscribe(() => IsAutoRecheckEnabled = false)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.Period)
                .Where(x => x != TimeSpan.Zero)
                .Where(x => x > maxValue || x < minValue)
                .Subscribe(() => Period = maxValue)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsAutoRecheckEnabled)
                .Subscribe(() => this.RaisePropertyChanged(nameof(Period)))
                .AddTo(Anchors);

            configProvider
                .WhenAnyValue(x => x.ActualConfig)
                .Select(x => x.MinRefreshTimeout)
                .Where(x => x != TimeSpan.Zero)
                .Where(x => x != TimeSpan.MinValue)
                .DistinctUntilChanged()
                .Subscribe(Reinitialize)
                .AddTo(Anchors);
        }

        public TimeSpan MinValue
        {
            get { return minValue; }
            set { this.RaiseAndSetIfChanged(ref minValue, value); }
        }

        public TimeSpan MaxValue
        {
            get { return maxValue; }
            set { this.RaiseAndSetIfChanged(ref maxValue, value); }
        }

        public TimeSpan Period
        {
            get { return period; }
            set { this.RaiseAndSetIfChanged(ref period, value); }
        }

        public bool IsAutoRecheckEnabled
        {
            get { return isAutoRecheckEnabled; }
            set { this.RaiseAndSetIfChanged(ref isAutoRecheckEnabled, value); }
        }

        private void Reinitialize(TimeSpan minRefreshTimeout)
        {
            MinValue = minRefreshTimeout;
        }
    }
}