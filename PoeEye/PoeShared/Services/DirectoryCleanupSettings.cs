namespace PoeShared.Services;

public sealed class DirectoryCleanupSettings
{
    public DirectoryCleanupSettings(DirectoryInfo directory)
    {
        Directory = directory ?? throw new ArgumentNullException(nameof(directory));
    }
    
    public DirectoryInfo Directory { get; }
    
    // Optional overrides; if null, service defaults are used
    public TimeSpan? WriteTimeToLive { get; set; }
    public TimeSpan? AccessTimeToLive { get; set; }
}
