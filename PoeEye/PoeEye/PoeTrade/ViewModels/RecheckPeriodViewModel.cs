using System;
using System.Reactive.Linq;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using PoeEye.Config;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using Prism.Commands;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    using IPoeEyeMainConfigProvider = IConfigProvider<PoeEyeMainConfig>;

    internal sealed class RecheckPeriodViewModel : DisposableReactiveObject, IRecheckPeriodViewModel
    {
        private static readonly TimeSpan DefaultMaxValue = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan DefaultMinValue = TimeSpan.FromSeconds(30);

        private TimeSpan maxValue = DefaultMaxValue;
        private TimeSpan minValue = DefaultMinValue;

        private TimeSpan period;

        public RecheckPeriodViewModel([NotNull] IPoeEyeMainConfigProvider configProvider)
        {
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));

            configProvider
                .WhenChanged
                .Select(x => new {x.MinRefreshTimeout, x.MaxRefreshTimeout})
                .DistinctUntilChanged()
                .Subscribe(x => Reinitialize(x.MinRefreshTimeout, x.MaxRefreshTimeout))
                .AddTo(Anchors);
            
            SetPeriodCommand = new DelegateCommand<object>(SetPeriodCommandExecuted);

            this.WhenAnyValue(x => x.Period)
                .Subscribe(() =>
                {
                    this.RaisePropertyChanged(nameof(IsLive));
                    this.RaisePropertyChanged(nameof(IsAutoRecheckEnabled));
                })
                .AddTo(Anchors);
        }

        private void SetPeriodCommandExecuted(object obj)
        {
            if (obj == null)
            {
                Period = TimeSpan.MinValue;
            } 
            else if (obj is double periodInSeconds)
            {
                Period = TimeSpan.FromSeconds(periodInSeconds);
            }
            else if (obj is TimeSpan period)
            {
                Period = period;
            }
        }

        public TimeSpan MinValue
        {
            get => minValue;
            set => this.RaiseAndSetIfChanged(ref minValue, value);
        }

        public TimeSpan MaxValue
        {
            get => maxValue;
            set => this.RaiseAndSetIfChanged(ref maxValue, value);
        }

        public TimeSpan Period
        {
            get => period;
            set => this.RaiseAndSetIfChanged(ref period, value);
        }

        public ICommand SetPeriodCommand { get; }

        public bool IsLive => Period == TimeSpan.Zero;

        public bool IsAutoRecheckEnabled => Period > TimeSpan.Zero;

        private TimeSpan MiddleSplit(TimeSpan min, TimeSpan max)
        {
            return TimeSpan.FromTicks(Math.Abs(max.Ticks - min.Ticks) / 2);
        }

        private void Reinitialize(TimeSpan minRefreshTimeout, TimeSpan maxRefreshTimeout)
        {
            MinValue = minRefreshTimeout == TimeSpan.Zero ? DefaultMinValue : minRefreshTimeout;
            MaxValue = maxRefreshTimeout == TimeSpan.Zero ? DefaultMaxValue : maxRefreshTimeout;

            if (Period <= TimeSpan.Zero)
            {
                return;
            }

            if (Period < MinValue || Period > MaxValue)
            {
                Period = MiddleSplit(MinValue, MaxValue);
            }
        }
    }
}