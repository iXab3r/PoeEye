namespace PoeShared.Scaffolding;

public readonly record struct RandomTimeSpan
{
    public RandomTimeSpan()
    {
    }

    public RandomTimeSpan(TimeSpan min)
    {
        Min = min;
    }

    public RandomTimeSpan(TimeSpan min, TimeSpan max)
    {
        Min = min;
        Max = max;
        Randomize = true;
    }
    
    public RandomTimeSpan(double minMs, double maxMs) : this(TimeSpan.FromMilliseconds(minMs), TimeSpan.FromMilliseconds(maxMs))
    {
    }

    /// <summary>
    /// Gets or sets the fixed delay duration. This is the default delay used if randomization is not enabled.
    /// </summary>
    public TimeSpan Min { get; init; } 
    
    /// <summary>
    /// Gets or sets the maximum delay duration used when delay randomization is enabled.
    /// </summary>
    public TimeSpan Max { get; init; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the delay should be randomized.
    /// When set to true, the actual delay will be a random value between <see cref="Min"/> and <see cref="Max"/>.
    /// </summary>
    public bool Randomize { get; init; }

    /// <summary>
    /// Returns next random timespan
    /// </summary>
    /// <returns></returns>
    public TimeSpan Next() => Randomize ? RandomNumberGenerator.Instance.NextTimeSpan(Min, Max) : Min;
}