using System;

namespace PoeEye.PoeTrade.ViewModels
{
    internal interface IRecheckPeriodViewModel
    {
        bool IsAutoRecheckEnabled { get; set; }

        TimeSpan Period { get; set; }
    }
}