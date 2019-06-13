using System;

namespace PoeShared
{
    internal sealed class Clock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}