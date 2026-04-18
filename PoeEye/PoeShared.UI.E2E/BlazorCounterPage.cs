namespace PoeShared.UI.E2E;

internal static class BlazorCounterPage
{
    public const string RootSelector = "[data-testid='blazor-root']";
    public const string TitleSelector = "[data-testid='counter-title']";
    public const string CounterSelector = "[data-testid='counter-value']";
    public const string IncrementButtonSelector = "[data-testid='increment-button']";
    public const string DisplayNameSelector = "[data-testid='counter-display-name']";
    public const string InstanceIdSelector = "[data-testid='counter-instance-id']";
    public const string ReactiveCounterSelector = "[data-testid='reactive-counter']";

    public const string ExpectedTitle = "Counter";
    public const string ExpectedInitialCounter = "0";
}
