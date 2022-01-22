using System;

namespace PoeShared;

public interface IClock
{
    DateTime UtcNow { get; }
        
    DateTime Now { get; }
        
    TimeSpan Elapsed { get; }
}