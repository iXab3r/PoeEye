using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using log4net;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;

namespace PoeShared.Dialogs.ViewModels
{
    internal abstract class MessageBoxViewModelBase : DisposableReactiveObject, IMessageBoxViewModel
    {
        private static readonly IFluentLog Log = typeof(MessageBoxViewModelBase).PrepareLogger();

        public MessageBoxViewModelBase()
        {
            CloseMessageBoxCommand = CommandWrapper.Create<MessageBoxElement?>(x =>
            {
                IsOpen = false;
                Result = x ?? default;
            });

            this.WhenAnyValue(x => x.IsOpen)
                .Where(x => x == false && AvailableCommands.Count == 0)
                .SubscribeSafe(() =>
                {
                    AvailableCommands.Add(MessageBoxElement.Close);
                }, Log.HandleUiException)
                .AddTo(Anchors);
        }

        public CommandWrapper CloseMessageBoxCommand { get; }

        public string Title { get; set; }

        public bool IsOpen { get; set; }

        public MessageBoxElement Result { get; private set; }
        
        public ObservableCollection<MessageBoxElement> AvailableCommands { get; } = new();

        public MessageBoxElement DefaultCommand => AvailableCommands.FirstOrDefault();
    }
}