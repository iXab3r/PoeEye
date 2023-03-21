﻿using System;
using System.Diagnostics;

namespace PoeShared.Audio.Models;

public class MaxSampleEventArgs : EventArgs
{
    [DebuggerStepThrough]
    public MaxSampleEventArgs(float minValue, float maxValue)
    {
        this.MaxSample = maxValue;
        this.MinSample = minValue;
    }
    public float MaxSample { get; private set; }
    public float MinSample { get; private set; }
}