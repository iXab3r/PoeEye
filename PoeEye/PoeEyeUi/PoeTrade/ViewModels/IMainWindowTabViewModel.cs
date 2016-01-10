namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.ComponentModel;

    internal interface IMainWindowTabViewModel : IDisposable, INotifyPropertyChanged
    {
        bool AudioNotificationEnabled { get; set; }

        IPoeTradesListViewModel TradesList { get; }

        IRecheckPeriodViewModel RecheckPeriod { get; }

        PoeQueryViewModel Query { get; }
    }
}