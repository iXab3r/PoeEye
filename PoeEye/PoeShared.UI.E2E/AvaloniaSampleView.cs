namespace PoeShared.UI.E2E;

internal enum AvaloniaSampleView
{
    Counter,
    CounterAlt,
    Slow,
    Broken
}

internal static class AvaloniaSampleViewExtensions
{
    public static string ToCommandLineKey(this AvaloniaSampleView sampleView)
    {
        return sampleView switch
        {
            AvaloniaSampleView.Counter => "counter",
            AvaloniaSampleView.CounterAlt => "counter-alt",
            AvaloniaSampleView.Slow => "slow",
            AvaloniaSampleView.Broken => "broken",
            _ => throw new ArgumentOutOfRangeException(nameof(sampleView), sampleView, null)
        };
    }
}
