using JetBrains.Annotations;
using PoeShared.Audio;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.UI.ViewModels
{
    public interface IAudioNotificationSelectorViewModel : IDisposableReactiveObject
    {
        AudioNotificationType SelectedValue { get; set; }

        IReactiveList<object> Items { [NotNull] get; }
    }
}