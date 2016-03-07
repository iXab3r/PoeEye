namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System.Windows.Input;

    using Config;

    using JetBrains.Annotations;

    using PoeShared.Scaffolding;

    internal interface IMainWindowTabViewModel : IDisposableReactiveObject
    {
        bool AudioNotificationEnabled { get; set; }

        bool IsBusy { get; }

        IPoeTradesListViewModel TradesList { [NotNull] get; }

        IRecheckPeriodViewModel RecheckPeriod { [NotNull] get; }

        ICommand MarkAllAsReadCommand { [NotNull] get; }

        PoeQueryViewModel Query { get; }

        void Load(PoeEyeTabConfig config);

        PoeEyeTabConfig Save();
    }
}