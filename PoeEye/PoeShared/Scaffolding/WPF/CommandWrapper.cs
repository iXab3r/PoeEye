using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
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
        private string description;

        public CommandWrapper(ReactiveCommand command)
        {
            Guard.ArgumentNotNull(command, nameof(command));

            this.command = command;

            isBusy = command.IsExecuting.ToProperty(this, x => x.IsBusy);
            command.ThrownExceptions.Subscribe(HandleException).AddTo(Anchors);
        }

        public static CommandWrapper Create(Func<Task> execute, IObservable<bool> canExecute)
        {
            return new CommandWrapper(ReactiveCommand.CreateFromTask(execute, canExecute));
        }
        
        public static CommandWrapper Create(Func<Task> execute)
        {
            return Create(execute, Observable.Return(true).Concat(Observable.Never<bool>()));
        }

        public bool IsBusy => isBusy.Value;

        public string Error
        {
            get { return error; }
            private set { this.RaiseAndSetIfChanged(ref error, value); }
        }

        public string Description
        {
            get { return description; }
            set { this.RaiseAndSetIfChanged(ref description, value); }
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
        
        private void HandleException(Exception exception)
        {
            Log.HandleUiException(exception);
            Error = exception.Message;
        }
    }
}