namespace PoeShared.UI;

public sealed record ApplicationStartupProperties
{
    public bool SkipInitialization { get; set; }
    public bool DoNotLoadLogConfigFromFile { get; set; }
}