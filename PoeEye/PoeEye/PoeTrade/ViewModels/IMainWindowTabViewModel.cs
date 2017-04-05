using PoeShared.Audio;
using PoeShared.PoeTrade;
using PoeShared.UI.ViewModels;

namespace PoeEye.PoeTrade.ViewModels
{
    using System.Windows.Input;

    using Config;

    using JetBrains.Annotations;

    using PoeShared.Scaffolding;

    internal interface IMainWindowTabViewModel : IDisposableReactiveObject
    {
        string Id { get; }

        bool IsBusy { get; }

        IPoeTradesListViewModel TradesList { [NotNull] get; }

        IRecheckPeriodViewModel RecheckPeriod { [NotNull] get; }

        ICommand MarkAllAsReadCommand { [NotNull] get; }

        ICommand RefreshCommand { [NotNull] get; }

        IPoeApiWrapper SelectedApi { get; }

        AudioNotificationType SelectedAudioNotificationType { get; }

        IPoeQueryViewModel Query { get; }

        string TabName { get; }

        void Load(PoeEyeTabConfig config);

        PoeEyeTabConfig Save();
    }
}