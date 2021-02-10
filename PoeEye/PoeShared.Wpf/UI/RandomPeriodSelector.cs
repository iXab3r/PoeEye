using System;
using System.Reactive.Linq;
using log4net;
using PoeShared.Scaffolding;

namespace PoeShared.UI
{
    internal sealed class RandomPeriodSelector : DisposableReactiveObject, IRandomPeriodSelector
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RandomPeriodSelector));

        private readonly IRandomNumberGenerator rng;

        private TimeSpan upperValue;
        private TimeSpan lowerValue;
        private bool randomizeValue;

        private TimeSpan minimum = TimeSpan.MinValue;
        private TimeSpan maximum = TimeSpan.MaxValue;
        
        public RandomPeriodSelector(IRandomNumberGenerator rng)
        {
            this.rng = rng;
            
            this.WhenAnyProperty(x => x.UpperValue)
                .Where(_ => !randomizeValue && upperValue != lowerValue)
                .SubscribeSafe(() => RandomizeValue = true, Log.HandleUiException)
                .AddTo(Anchors);
            
            this.WhenAnyProperty(x => x.UpperValue, x => x.RandomizeValue)
                .Where(x => lowerValue > upperValue || !randomizeValue)
                .SubscribeSafe(x => LowerValue = upperValue, Log.HandleUiException)
                .AddTo(Anchors);
            
            this.WhenAnyProperty(x => x.LowerValue, x => x.RandomizeValue)
                .Where(x => upperValue < lowerValue || !randomizeValue)
                .SubscribeSafe(x => UpperValue = lowerValue, Log.HandleUiException)
                .AddTo(Anchors);
        }

        public TimeSpan Minimum
        {
            get => minimum;
            set => RaiseAndSetIfChanged(ref minimum, value);
        }

        public TimeSpan Maximum
        {
            get => maximum;
            set => RaiseAndSetIfChanged(ref maximum, value);
        }

        public TimeSpan LowerValue
        {
            get => lowerValue;
            set => this.RaiseAndSetIfChanged(ref lowerValue, value.EnsureInRange(minimum, maximum));
        }

        public TimeSpan UpperValue
        {
            get => upperValue;
            set => RaiseAndSetIfChanged(ref upperValue, value.EnsureInRange(minimum, maximum));
        }
        
        public bool RandomizeValue
        {
            get => randomizeValue;
            set => RaiseAndSetIfChanged(ref randomizeValue, value);
        }

        public TimeSpan GetValue()
        {
            return randomizeValue ? rng.GenerateDelay(lowerValue, upperValue) : lowerValue;
        }
    }
}