namespace PoeShared.Services;

public interface IFolderCleaner : IDisposableReactiveObject
{
    string Name { get; set; }
    
    TimeSpan? FileTimeToLive { get; set; }
    
    TimeSpan? FileAccessTimeToLive { get; set; }
    
    TimeSpan? CleanupTimeout { get; set; }
        
    IDisposable AddDirectory(DirectoryInfo directoryInfo);
    
    IDisposable AddDirectory(DirectoryCleanupSettings settings);
}