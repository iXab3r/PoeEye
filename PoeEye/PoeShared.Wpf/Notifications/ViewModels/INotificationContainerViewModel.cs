using System;
using System.Windows.Input;
using System.Windows.Media;
using PoeShared.Scaffolding;

namespace PoeShared.Notifications.ViewModels
{
    internal interface INotificationContainerViewModel : IDisposableReactiveObject
    {
        TimeSpan TimeLeft { get; }
        
        ICommand CloseCommand { get; }
        
        string Title { get; }
        
        ImageSource Icon { get; }
    }
}