namespace PoeShared
{
    using System;

    public interface IClock
    {
        DateTime CurrentTime { get; }
    }
}