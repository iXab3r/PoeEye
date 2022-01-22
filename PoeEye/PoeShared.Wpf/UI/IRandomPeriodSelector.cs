using System;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.UI;

public interface IRandomPeriodSelector : IDisposableReactiveObject
{
    TimeSpan Minimum { get; set; }
    TimeSpan Maximum { get; set; }
    TimeSpan LowerValue { get; set; }
    TimeSpan UpperValue { get; set; }
    bool RandomizeValue { get; set; }
    TimeSpan GetValue();
}