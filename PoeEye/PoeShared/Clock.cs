namespace PoeShared
{
    using System;

    internal sealed class Clock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}