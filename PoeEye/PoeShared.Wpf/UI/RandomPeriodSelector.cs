using System;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Wpf.Scaffolding;
using PropertyBinder;

namespace PoeShared.UI
{
    internal sealed class RandomPeriodSelector : ValidatableReactiveObject<RandomPeriodSelector>, IRandomPeriodSelector
    {
        private static readonly IFluentLog Log = typeof(RandomPeriodSelector).PrepareLogger();
        private static readonly Binder<RandomPeriodSelector> Binder = new();

        private readonly IRandomNumberGenerator rng;

        private TimeSpan upperValue;
        private TimeSpan lowerValue;
        private bool randomizeValue;

        private TimeSpan minimum = TimeSpan.Zero;
        private TimeSpan maximum = TimeSpan.MaxValue;

        static RandomPeriodSelector()
        {
            Binder.BindIf( x => !x.LowerValue.IsInRange(x.Minimum, x.Maximum), x => x.LowerValue.EnsureInRange(x.Minimum, x.Maximum)).To(x => x.LowerValue);
            Binder
                .BindIf( x => !x.UpperValue.IsInRange(x.Minimum, x.Maximum),x => x.UpperValue.EnsureInRange(x.Minimum, x.Maximum))
                .ElseIf(x => x.RandomizeValue == false, x => x.LowerValue)
                .ElseIf(x => x.RandomizeValue == true && x.LowerValue > x.UpperValue, x => x.LowerValue)
                .To(x => x.UpperValue);
        }

        public RandomPeriodSelector(IRandomNumberGenerator rng)
        {
            this.rng = rng;
            
            this.ValidationRule(x => x.UpperValue,
                x => !randomizeValue || x >= lowerValue, x => $"Must be greater than {lowerValue}");
            
            Binder.Attach(this).AddTo(Anchors);
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
            set => this.RaiseAndSetIfChanged(ref lowerValue, value);
        }

        public TimeSpan UpperValue
        {
            get => upperValue;
            set => RaiseAndSetIfChanged(ref upperValue, value);
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

        public override string ToString()
        {
            return $"RandomSelector([{lowerValue};{upperValue}], randomize: {randomizeValue})";
        }
    }
}