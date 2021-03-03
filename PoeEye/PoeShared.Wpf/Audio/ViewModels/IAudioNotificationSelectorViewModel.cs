using System.Collections.ObjectModel;
using System.Windows.Input;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Audio.ViewModels
{
    public interface IAudioNotificationSelectorViewModel : IDisposableReactiveObject
    {
        string SelectedValue { get; set; }
        
        ICommand PlayNotificationCommand { get; }
        
        ICommand SelectNotificationCommand { get; }
    }
}