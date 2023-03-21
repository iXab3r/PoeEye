using System;
using System.Diagnostics;
using NAudio.Dsp;

namespace PoeShared.Audio.Models;

public class FftEventArgs : EventArgs
{
    [DebuggerStepThrough]
    public FftEventArgs(Complex[] result)
    {
        this.Result = result;
    }
    public Complex[] Result { get; private set; }
}