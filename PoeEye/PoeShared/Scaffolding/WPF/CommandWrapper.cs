using System;
using System.Reactive.Linq;
using System.Windows.Input;
using Guards;
using ReactiveUI;

namespace PoeShared.Scaffolding.WPF
{
    public sealed class CommandWrapper : DisposableReactiveObject, ICommand
    {
        private readonly ReactiveCommand command;
        private readonly ObservableAsPropertyHelper<bool> isBusy;
        private string error;
        
        public CommandWrapper(ReactiveCommand command)
        {
            Guard.ArgumentNotNull(command, nameof(command));

            this.command = command;

            isBusy = command.IsExecuting.ToProperty(this, x => x.IsBusy);
            command.ThrownExceptions.Subscribe(x => Error = x.Message).AddTo(Anchors);
        }
        
        public bool IsBusy => isBusy.Value;


        public string Error
        {
            get { return error; }
            private set { this.RaiseAndSetIfChanged(ref error, value); }
        }
        
        private ICommand InnerCommand => command;

        public bool CanExecute(object parameter)
        {
            return ((ICommand)command).CanExecute(parameter);
        }

        public void Execute(object parameter)
        {
            Error = null;
            ((ICommand)command).Execute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add => InnerCommand.CanExecuteChanged += value;
            remove => InnerCommand.CanExecuteChanged -= value;
        }
    }
}