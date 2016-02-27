namespace PoeShared
{
    using System;

    internal sealed class Clock : IClock
    {
        public DateTime CurrentTime => DateTime.Now;
    }
}