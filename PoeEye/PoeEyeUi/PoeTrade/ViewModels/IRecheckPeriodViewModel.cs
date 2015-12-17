namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;

    internal interface IRecheckPeriodViewModel
    {
        bool IsAutoRecheckEnabled { get; set; }

        TimeSpan RecheckValue { get; set; }
    }
}