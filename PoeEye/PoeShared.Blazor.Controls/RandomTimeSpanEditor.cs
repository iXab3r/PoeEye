using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Controls;

partial class RandomTimeSpanEditor
{
    [Parameter] public RandomTimeSpan Value { get; set; }

    [Parameter] public TimeSpan Step { get; set; } = TimeSpan.FromMilliseconds(10);

    [Parameter] public TimeSpan Minimum { get; set; } = TimeSpan.Zero;

    [Parameter] public TimeSpan Maximum { get; set; } = TimeSpan.MaxValue;

    [Parameter] public EventCallback<RandomTimeSpan> ValueChanged { get; set; }

    [Parameter] public bool CanRandomize { get; set; } = true;

    public bool IsRandom { get; private set; }

    private double MinimumMilliseconds => Minimum.TotalMilliseconds;

    private double? MaximumMilliseconds => Maximum == TimeSpan.MaxValue
        ? null
        : Maximum.TotalMilliseconds;

    private double UpperMinimumMilliseconds => Math.Max(MinimumMilliseconds, Value.Min.TotalMilliseconds);

    private static TimeSpan ParseMs(object value)
    {
        return value is not string valueAsString || string.IsNullOrEmpty(valueAsString)
            ? TimeSpan.Zero
            : TimeSpan.FromMilliseconds(Convert.ToDouble(valueAsString));
    }

    private Task HandleMinChanged(ChangeEventArgs args)
    {
        var nextMin = Coerce(ParseMs(args.Value), Minimum, Maximum);
        var nextValue = Value with {Min = nextMin};
        if (nextValue.Randomize && nextValue.Max < nextMin)
        {
            nextValue = nextValue with {Max = nextMin};
        }

        return ValueChanged.InvokeAsync(nextValue);
    }

    private Task HandleMaxChanged(ChangeEventArgs args)
    {
        var nextMax = Coerce(ParseMs(args.Value), nextMin: Value.Min, nextMax: Maximum);
        return ValueChanged.InvokeAsync(Value with {Max = nextMax});
    }

    private Task HandleRandomizeChanged()
    {
        return ValueChanged.InvokeAsync(Coerce(Value with {Randomize = IsRandom}));
    }

    private RandomTimeSpan Coerce(RandomTimeSpan value)
    {
        var nextMin = Coerce(value.Min, Minimum, Maximum);
        var nextMax = Coerce(value.Max, nextMin, Maximum);
        return value with
        {
            Min = nextMin,
            Max = nextMax,
        };
    }

    private static TimeSpan Coerce(TimeSpan value, TimeSpan nextMin, TimeSpan nextMax)
    {
        if (nextMax < nextMin)
        {
            return nextMin;
        }

        if (value < nextMin)
        {
            return nextMin;
        }

        if (value > nextMax)
        {
            return nextMax;
        }

        return value;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        IsRandom = Value.Randomize;
    }
}
