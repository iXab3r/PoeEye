namespace PoeShared.UI.Avalonia;

public sealed record AvaloniaSampleDefinition(
    string Key,
    string DisplayName,
    Type ViewType,
    bool AcceptsDataContext);

public static class AvaloniaSampleRegistry
{
    public static IReadOnlyList<AvaloniaSampleDefinition> All { get; } =
    [
        new(AvaloniaAppOptions.SampleCounter, "Counter", typeof(Blazor.MainCounterView), true),
        new(AvaloniaAppOptions.SampleCounterAlt, "Counter Alt", typeof(Blazor.MainCounterViewAlt), true),
        new(AvaloniaAppOptions.SampleSlow, "Slow", typeof(Blazor.SlowView), false),
        new(AvaloniaAppOptions.SampleBroken, "Broken", typeof(Blazor.BrokenView), false)
    ];

    public static AvaloniaSampleDefinition Resolve(string? key)
    {
        var normalizedKey = string.IsNullOrWhiteSpace(key)
            ? AvaloniaAppOptions.SampleCounter
            : key.Trim().ToLowerInvariant();

        return All.FirstOrDefault(x => string.Equals(x.Key, normalizedKey, StringComparison.OrdinalIgnoreCase))
               ?? All[0];
    }
}
