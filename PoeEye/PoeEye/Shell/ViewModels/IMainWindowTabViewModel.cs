using System.Windows.Input;
using JetBrains.Annotations;
using PoeEye.Config;
using PoeShared.Audio;
using PoeShared.Audio.Services;
using PoeShared.PoeTrade;
using PoeShared.Scaffolding;

namespace PoeEye.PoeTrade.ViewModels
{
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