using System.Collections.ObjectModel;
using System.Windows.Input;
using JetBrains.Annotations;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using ReactiveUI;

namespace PoeShared.Audio.ViewModels
{
    public interface IAudioNotificationSelectorViewModel : IDisposableReactiveObject
    {
        string SelectedValue { get; set; }
        
        float Volume { get; set; }
        
        ICommand PlayNotificationCommand { get; }
        
        ICommand SelectNotificationCommand { get; }
    }
}