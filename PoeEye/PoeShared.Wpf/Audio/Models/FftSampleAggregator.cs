using System;
using NAudio.Dsp;

namespace PoeShared.Audio.Models;

public sealed class FftSampleAggregator
{
    private readonly FftEventArgs fftArgs;
    private readonly Complex[] fftBuffer;
    private readonly int m;
    private int count;
    private int fftLength;
    private int fftPos;
    private float maxValue;
    private float minValue;

    public FftSampleAggregator(int fftLength = 1024)
    {
        if (!IsPowerOfTwo(fftLength))
        {
            throw new ArgumentException("FFT Length must be a power of two");
        }

        m = (int) Math.Log(fftLength, 2.0);
        this.fftLength = fftLength;
        fftBuffer = new Complex[fftLength];
        fftArgs = new FftEventArgs(fftBuffer);
    }

    public int NotificationCount { get; set; }

    public bool PerformFFT { get; set; }

    // volume
    public event EventHandler<MaxSampleEventArgs> MaximumCalculated;

    // FFT
    public event EventHandler<FftEventArgs> FftCalculated;

    private static bool IsPowerOfTwo(int x)
    {
        return (x & (x - 1)) == 0;
    }

    public void Reset()
    {
        count = 0;
        maxValue = minValue = 0;
    }

    public void Add(float value)
    {
        if (PerformFFT && FftCalculated != null)
        {
            fftBuffer[fftPos].X = (float) (value * FastFourierTransform.HammingWindow(fftPos, fftBuffer.Length));
            fftBuffer[fftPos].Y = 0;
            fftPos++;
            if (fftPos >= fftBuffer.Length)
            {
                fftPos = 0;
                // 1024 = 2^10
                FastFourierTransform.FFT(true, m, fftBuffer);
                FftCalculated(this, fftArgs);
            }
        }

        maxValue = Math.Max(maxValue, value);
        minValue = Math.Min(minValue, value);
        count++;
        if (count < NotificationCount || NotificationCount <= 0)
        {
            return;
        }

        MaximumCalculated?.Invoke(this, new MaxSampleEventArgs(minValue, maxValue));
        Reset();
    }
}