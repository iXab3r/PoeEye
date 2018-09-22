using System;

namespace PoeEye.PoeTrade.ViewModels
{
    internal interface IRecheckPeriodViewModel
    {
        TimeSpan Period { get; set; }

        bool IsLive { get; }

        bool IsAutoRecheckEnabled { get; }
    }
}