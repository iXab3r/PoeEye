namespace PoeEye.PoeTrade.ViewModels
{
    using System;

    internal interface IRecheckPeriodViewModel
    {
        bool IsAutoRecheckEnabled { get; set; }

        TimeSpan Period { get; set; }
    }
}