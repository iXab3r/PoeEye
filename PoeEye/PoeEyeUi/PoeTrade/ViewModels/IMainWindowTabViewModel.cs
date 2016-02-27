namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.ComponentModel;

    using Config;

    internal interface IMainWindowTabViewModel : IDisposable, INotifyPropertyChanged
    {
        bool AudioNotificationEnabled { get; set; }

        IPoeTradesListViewModel TradesList { get; }

        IRecheckPeriodViewModel RecheckPeriod { get; }

        PoeQueryViewModel Query { get; }

        void Load(PoeEyeTabConfig config);

        PoeEyeTabConfig Save();
    }
}