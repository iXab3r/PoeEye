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

        private bool isOpen;
        private string title;
        private MessageBoxElement result;

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

        public string Title
        {
            get => title;
            set => RaiseAndSetIfChanged(ref title, value);
        }

        public bool IsOpen
        {
            get => isOpen;
            set => RaiseAndSetIfChanged(ref isOpen, value);
        }

        public MessageBoxElement Result
        {
            get => result;
            private set => RaiseAndSetIfChanged(ref result, value);
        }
        
        public ObservableCollection<MessageBoxElement> AvailableCommands { get; } = new();

        public MessageBoxElement DefaultCommand => AvailableCommands.FirstOrDefault();
    }
}