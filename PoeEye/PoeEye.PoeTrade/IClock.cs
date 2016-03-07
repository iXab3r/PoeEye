namespace PoeEye
{
    using System;

    public interface IClock
    {
        DateTime CurrentTime { get; }
    }
}