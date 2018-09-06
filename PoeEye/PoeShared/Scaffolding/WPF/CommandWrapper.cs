using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Input;
using Guards;
using Prism.Commands;
using ReactiveUI;

namespace PoeShared.Scaffolding.WPF
{
    public sealed class CommandWrapper : DisposableReactiveObject, ICommand
    {
        private readonly ICommand command;
        private readonly ObservableAsPropertyHelper<bool> isBusy;
        private string error;
        private string description;

        private readonly ISubject<Unit> raiseCanExecuteChangedRequests = new Subject<Unit>();

        public CommandWrapper(ReactiveCommand command)
        {
            this.command = command;

            isBusy = command.IsExecuting.ToProperty(this, x => x.IsBusy);
            command.ThrownExceptions.Subscribe(HandleException).AddTo(Anchors);

            raiseCanExecuteChangedRequests
                .Subscribe(() => throw new NotSupportedException($"RaiseCanExecuteChanged is not supported for commands of type {command}"))
                .AddTo(Anchors);
        }

        private CommandWrapper(DelegateCommandBase command)
        {
            this.command = command;

            isBusy = Observable.Return(false).ToProperty(this, x => x.IsBusy);
            raiseCanExecuteChangedRequests
                .Subscribe(command.RaiseCanExecuteChanged)
                .AddTo(Anchors);
        }
        
        public static CommandWrapper Create<T>(DelegateCommand<T> command)
        {
            return new CommandWrapper(command);
        }
        
        public static CommandWrapper Create(DelegateCommand command)
        {
            return new CommandWrapper(command);
        }
        
        public static CommandWrapper Create(ReactiveCommand command)
        {
            return new CommandWrapper(command);
        }

        public static CommandWrapper Create(Func<Task> execute, IObservable<bool> canExecute)
        {
            return new CommandWrapper(ReactiveCommand.CreateFromTask(execute, canExecute));
        }
        
        public static CommandWrapper Create(Func<Task> execute)
        {
            return Create(execute, Observable.Return(true).Concat(Observable.Never<bool>()));
        }
        
        public static CommandWrapper Create<TParam>(Func<TParam, Task> execute, IObservable<bool> canExecute)
        {
            return new CommandWrapper(ReactiveCommand.CreateFromTask(execute, canExecute));
        }
        
        public static CommandWrapper Create<TParam>(Func<TParam, Task> execute)
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
            return command.CanExecute(parameter);
        }

        public void Execute(object parameter)
        {
            Error = null;
            try
            {
                command.Execute(parameter);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
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

        public void RaiseCanExecuteChanged()
        {
            raiseCanExecuteChangedRequests.OnNext(Unit.Default);
        }
    }
}