namespace PoeShared.Scaffolding;

public readonly record struct RandomInteger
{
    public RandomInteger()
    {
    }

    public RandomInteger(int min)
    {
        Min = min;
    }

    public RandomInteger(int min, int max)
    {
        Min = min;
        Max = max;
        Randomize = true;
    }

    public int Min { get; init; } 
   
    public int Max { get; init; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the value should be randomized.
    /// When set to true, the actual delay will be a random value between <see cref="Min"/> and <see cref="Max"/>.
    /// </summary>
    public bool Randomize { get; init; }
}