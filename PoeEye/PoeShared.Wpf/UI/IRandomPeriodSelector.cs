using System;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

/// <summary>
/// Defines an interface for selecting a time period, with optional randomization within specified bounds.
/// This interface is useful in scenarios where a time duration needs to be determined, potentially with variability.
/// </summary>
public interface IRandomPeriodSelector : IDisposableReactiveObject
{
    /// <summary>
    /// Gets or sets the minimum allowable time span. This value bounds the lower limit of the time period selection.
    /// </summary>
    TimeSpan Minimum { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowable time span. This value bounds the upper limit of the time period selection.
    /// </summary>
    TimeSpan Maximum { get; set; }

    /// <summary>
    /// Gets or sets the lower value of the time period. If <see cref="RandomizeValue"/> is false, this is the value returned by <see cref="GetValue"/>.
    /// </summary>
    TimeSpan LowerValue { get; set; }

    /// <summary>
    /// Gets or sets the upper value of the time period. If <see cref="RandomizeValue"/> is true, the value returned by <see cref="GetValue"/> will be within this upper bound and the <see cref="LowerValue"/>.
    /// </summary>
    TimeSpan UpperValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the time period should be randomized within the bounds of <see cref="LowerValue"/> and <see cref="UpperValue"/>.
    /// </summary>
    bool RandomizeValue { get; set; }

    /// <summary>
    /// Retrieves the selected time period value. If <see cref="RandomizeValue"/> is true, the value will be a random TimeSpan within the range defined by <see cref="LowerValue"/> and <see cref="UpperValue"/>; otherwise, it returns the <see cref="LowerValue"/>.
    /// </summary>
    /// <returns>A TimeSpan representing the selected time period.</returns>
    TimeSpan GetValue();
}