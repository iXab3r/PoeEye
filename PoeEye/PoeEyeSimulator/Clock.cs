﻿namespace PoeEye.Simulator
{
    using System;

    using PoeShared;

    internal sealed class Clock : IClock
    {
        public DateTime CurrentTime => DateTime.Now;
    }
}