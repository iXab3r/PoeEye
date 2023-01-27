namespace PoeShared.Logging;

public sealed class FluentLogSettings
{
    private static readonly Lazy<FluentLogSettings> InstanceSupplier = new Lazy<FluentLogSettings>();

    public static FluentLogSettings Instance => InstanceSupplier.Value;
    
    public FluentLogLevel? MinLogLevel { get; set; }
}