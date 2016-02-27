namespace PoeEyeUi.PoeTrade.ViewModels
{
    using Config;

    using PoeShared.Scaffolding;

    internal interface IMainWindowTabViewModel : IDisposableReactiveObject
    {
        bool AudioNotificationEnabled { get; set; }

        IPoeTradesListViewModel TradesList { get; }

        IRecheckPeriodViewModel RecheckPeriod { get; }

        PoeQueryViewModel Query { get; }

        void Load(PoeEyeTabConfig config);

        PoeEyeTabConfig Save();
    }
}