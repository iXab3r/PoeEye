using System.Windows.Input;
using PoeShared.Scaffolding;

namespace PoeShared.Audio.ViewModels;

public interface IAudioNotificationSelectorViewModel : IDisposableReactiveObject
{
    string SelectedValue { get; set; }
        
    float Volume { get; set; }
        
    ICommand PlayNotificationCommand { get; }
        
    ICommand SelectNotificationCommand { get; }
}