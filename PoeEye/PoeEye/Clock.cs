namespace PoeEye
{
    using System;

    using PoeShared;

    internal sealed class Clock : IClock
    {
        public DateTime CurrentTime => DateTime.Now;
    }
}