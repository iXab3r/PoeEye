using System.Collections.ObjectModel;
using JetBrains.Annotations;
using PoeShared.Audio.Services;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Audio.ViewModels
{
    public interface IAudioNotificationSelectorViewModel : IDisposableReactiveObject
    {
        string SelectedValue { get; set; }

        AudioNotificationType SelectedItem { get; set; }

        ReadOnlyObservableCollection<object> Items { [NotNull] get; }
    }
}