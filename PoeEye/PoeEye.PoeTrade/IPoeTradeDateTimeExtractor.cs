using System;
using JetBrains.Annotations;

namespace PoeEye.PoeTrade
{
    internal interface IPoeTradeDateTimeExtractor
    {
        DateTime? ExtractTimestamp([CanBeNull] string timestamp);
    }
}