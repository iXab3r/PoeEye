namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Reactive.Linq;

    using PoeShared.Utilities;

    using ReactiveUI;

    internal sealed class RecheckPeriodViewModel : DisposableReactiveObject, IRecheckPeriodViewModel
    {
        private bool isAutoRecheckEnabled;

        private TimeSpan maxValue = TimeSpan.FromMinutes(30);

        private TimeSpan minValue = TimeSpan.FromMinutes(5);

        private TimeSpan recheckValue = TimeSpan.FromMinutes(5);

        public RecheckPeriodViewModel()
        {
            this.WhenAnyValue(x => x.RecheckValue)
                .Where(x => x == TimeSpan.Zero)
                .Subscribe(() => IsAutoRecheckEnabled = false)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.RecheckValue)
                .Where(x => x != TimeSpan.Zero)
                .Where(x => x > maxValue || x < minValue)
                .Subscribe(() => RecheckValue = maxValue)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsAutoRecheckEnabled)
                .Subscribe(() => this.RaisePropertyChanged(nameof(RecheckValue)))
                .AddTo(Anchors);
        }

        public TimeSpan RecheckValue
        {
            get { return recheckValue; }
            set { this.RaiseAndSetIfChanged(ref recheckValue, value); }
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

        public bool IsAutoRecheckEnabled
        {
            get { return isAutoRecheckEnabled; }
            set { this.RaiseAndSetIfChanged(ref isAutoRecheckEnabled, value); }
        }
    }
}