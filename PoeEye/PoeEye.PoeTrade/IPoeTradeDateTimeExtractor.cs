using System;

namespace PoeEye.PoeTrade
{
    using JetBrains.Annotations;

    internal interface IPoeTradeDateTimeExtractor
    {
        DateTime? ExtractTimestamp([CanBeNull] string timestamp);
    }
}