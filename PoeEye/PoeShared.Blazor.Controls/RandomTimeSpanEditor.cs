using System;
using Microsoft.AspNetCore.Components;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Controls;

partial class RandomTimeSpanEditor
{
    [Parameter] public RandomTimeSpan Value { get; set; }

    [Parameter] public TimeSpan Step { get; set; } = TimeSpan.FromMilliseconds(10);

    [Parameter] public EventCallback<RandomTimeSpan> ValueChanged { get; set; }

    [Parameter] public bool CanRandomize { get; set; } = true;

    public bool IsRandom { get; private set; }
    

    private static TimeSpan ParseMs(object value)
    {
        return value is not string valueAsString || string.IsNullOrEmpty(valueAsString)
            ? TimeSpan.Zero
            : TimeSpan.FromMilliseconds(Convert.ToDouble(valueAsString));
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        IsRandom = Value.Randomize;
    }
}