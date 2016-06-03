using JetBrains.Annotations;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    internal interface IAudioNotificationSelectorViewModel : IDisposableReactiveObject
    {
        AudioNotificationType SelectedValue { get; set; }

        IReactiveList<object> Items { [NotNull] get; }
    }
}