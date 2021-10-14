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

        static RandomPeriodSelector()
        {
            Binder
                .BindIf( x => !x.LowerValue.IsInRange(x.Minimum, x.Maximum), x => x.LowerValue.EnsureInRange(x.Minimum, x.Maximum))
                .To(x => x.LowerValue);
            Binder
                .BindIf( x => !x.UpperValue.IsInRange(x.Minimum, x.Maximum),x => x.UpperValue.EnsureInRange(x.Minimum, x.Maximum))
                .ElseIf(x => x.RandomizeValue == true && x.LowerValue > x.UpperValue, x => x.LowerValue)
                .To(x => x.UpperValue);
        }

        public RandomPeriodSelector(IRandomNumberGenerator rng)
        {
            this.rng = rng;
            
            this.ValidationRule(x => x.UpperValue,
                x => !RandomizeValue || x >= LowerValue, x => $"Must be greater than {LowerValue}");
            
            Binder.Attach(this).AddTo(Anchors);
        }

        public TimeSpan Minimum { get; set; } = TimeSpan.Zero;

        public TimeSpan Maximum { get; set; } = TimeSpan.MaxValue;

        public TimeSpan LowerValue { get; set; }

        public TimeSpan UpperValue { get; set; }
        
        public bool RandomizeValue { get; set; }

        public TimeSpan GetValue()
        {
            if (RandomizeValue && UpperValue < LowerValue)
            {
                throw new InvalidOperationException($"Invalid range: [{LowerValue}; {UpperValue}]");
            }
            return RandomizeValue ? rng.GenerateDelay(LowerValue, UpperValue) : LowerValue;
        }

        public override string ToString()
        {
            return $"RandomSelector([{LowerValue};{UpperValue}], randomize: {RandomizeValue})";
        }
    }
}