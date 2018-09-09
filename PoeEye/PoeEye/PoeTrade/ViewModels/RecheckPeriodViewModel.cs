using System;
using System.Reactive.Linq;
using Guards;
using JetBrains.Annotations;
using PoeEye.Config;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    using IPoeEyeMainConfigProvider = IConfigProvider<PoeEyeMainConfig>;

    internal sealed class RecheckPeriodViewModel : DisposableReactiveObject, IRecheckPeriodViewModel
    {
        private static readonly TimeSpan DefaultMaxValue = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan DefaultMinValue = TimeSpan.FromSeconds(30);

        private bool isAutoRecheckEnabled;

        private TimeSpan maxValue = DefaultMaxValue;

        private TimeSpan minValue = DefaultMinValue;

        private TimeSpan period;

        public RecheckPeriodViewModel([NotNull] IPoeEyeMainConfigProvider configProvider)
        {
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));

            this.WhenAnyValue(x => x.Period)
                .Where(x => x == TimeSpan.Zero)
                .Subscribe(() => IsAutoRecheckEnabled = false)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.Period)
                .Where(x => x != TimeSpan.Zero)
                .Where(x => x > maxValue || x < minValue)
                .Subscribe(() => Period = MiddleSplit(minValue, maxValue))
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsAutoRecheckEnabled)
                .Subscribe(() => this.RaisePropertyChanged(nameof(Period)))
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsAutoRecheckEnabled)
                .Where(x => x)
                .Where(x => period == TimeSpan.Zero)
                .Subscribe(() => Period = MiddleSplit(minValue, maxValue))
                .AddTo(Anchors);

            configProvider
                .WhenChanged
                .Select(x => new { x.MinRefreshTimeout, x.MaxRefreshTimeout })
                .DistinctUntilChanged()
                .Subscribe(x => Reinitialize(x.MinRefreshTimeout, x.MaxRefreshTimeout))
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

        private TimeSpan MiddleSplit(TimeSpan min, TimeSpan max)
        {
            return TimeSpan.FromTicks(Math.Abs(max.Ticks - min.Ticks) / 2);
        }

        private void Reinitialize(TimeSpan minRefreshTimeout, TimeSpan maxRefreshTimeout)
        {
            MinValue = minRefreshTimeout == TimeSpan.Zero ? DefaultMinValue : minRefreshTimeout;
            MaxValue = maxRefreshTimeout == TimeSpan.Zero ? DefaultMaxValue : maxRefreshTimeout;

            this.RaisePropertyChanged(nameof(Period));
        }
    }
}
