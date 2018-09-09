using System;

namespace PoeShared
{
    public interface IClock
    {
        DateTime Now { get; }
    }
}