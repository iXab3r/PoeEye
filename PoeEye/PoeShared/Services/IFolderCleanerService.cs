using System;
using System.IO;

namespace PoeShared.Services
{
    public interface IFolderCleanerService 
    {
        TimeSpan? FileTimeToLive { get; set; }
        
        TimeSpan? CleanupTimeout { get; set; }
        
        IDisposable AddDirectory(DirectoryInfo directoryInfo);
    }
}