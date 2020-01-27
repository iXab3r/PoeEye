using System;

namespace PoeShared
{
    internal sealed class Clock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
        
        public DateTime Now => DateTime.Now;
    }
}