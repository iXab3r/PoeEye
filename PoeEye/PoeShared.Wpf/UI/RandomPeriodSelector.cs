using System;
using System.ComponentModel;
using System.Reactive.Linq;
using log4net;
using PoeShared.Scaffolding;
using PoeShared.Wpf.Scaffolding;
using PropertyBinder;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Components.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;

namespace PoeShared.UI
{
    internal sealed class RandomPeriodSelector : ValidatableReactiveObject<RandomPeriodSelector>, IRandomPeriodSelector
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RandomPeriodSelector));

        private readonly IRandomNumberGenerator rng;

        private TimeSpan upperValue;
        private TimeSpan lowerValue;
        private bool randomizeValue;

        private TimeSpan minimum = TimeSpan.Zero;
        private TimeSpan maximum = TimeSpan.MaxValue;

        public RandomPeriodSelector(IRandomNumberGenerator rng)
        {
            this.rng = rng;
            
            this.WhenAnyValue(x => x.UpperValue)
                .Where(_ => !randomizeValue && upperValue != lowerValue)
                .SubscribeSafe(() => RandomizeValue = true, Log.HandleUiException)
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.RandomizeValue)
                .WithPrevious()
                .Where(x => x.Previous == false && x.Current == true)
                .Where(x => upperValue < lowerValue)
                .SubscribeSafe(() => UpperValue = lowerValue, Log.HandleUiException)
                .AddTo(Anchors);

            this.ValidationRule(x => x.UpperValue,
                x => !randomizeValue || x >= lowerValue, x => $"Must be greater than {lowerValue}");
            this.ValidationRule(x => x.LowerValue,
                x => !randomizeValue || x <= upperValue, x => $"Must be less than {upperValue}");
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
            if (randomizeValue && upperValue < lowerValue)
            {
                throw new InvalidOperationException($"Invalid range: [{lowerValue}; {upperValue}]");
            }
            return randomizeValue ? rng.GenerateDelay(lowerValue, upperValue) : lowerValue;
        }
    }
}