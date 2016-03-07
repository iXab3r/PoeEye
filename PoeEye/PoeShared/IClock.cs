namespace PoeShared
{
    using System;

    public interface IClock
    {
        DateTime Now { get; }
    }
}