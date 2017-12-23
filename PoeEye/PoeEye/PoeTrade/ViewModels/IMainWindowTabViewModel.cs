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

        bool IsFlipped { get; set; }

        IPoeTradesListViewModel TradesList { [NotNull] get; }

        IRecheckPeriodViewModel RecheckPeriod { [NotNull] get; }

        ICommand MarkAllAsReadCommand { [NotNull] get; }

        ICommand RefreshCommand { [NotNull] get; }

        ICommand RenameCommand { [NotNull] get; }

        IPoeApiWrapper SelectedApi { [CanBeNull] get; }

        AudioNotificationType SelectedAudioNotificationType { get; }

        IPoeQueryViewModel Query { [NotNull] get; }

        string TabName { [NotNull] get; }

        void Load(PoeEyeTabConfig config);

        PoeEyeTabConfig Save();
    }
}