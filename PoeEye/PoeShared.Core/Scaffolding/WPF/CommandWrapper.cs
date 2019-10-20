using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Input;

using log4net;
using Prism.Commands;
using ReactiveUI;

namespace PoeShared.Scaffolding.WPF
{
    public sealed class CommandWrapper : DisposableReactiveObject, ICommand
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CommandWrapper));

        private readonly ISubject<bool> isExecuting = new Subject<bool>();

        private readonly ISubject<Unit> raiseCanExecuteChangedRequests = new Subject<Unit>();
        private string description;
        private string error;
        private bool isBusy;
        
        private CommandWrapper(DelegateCommandBase command)
        {
            Guard.ArgumentNotNull(command, nameof(command));
            
            InnerCommand = command;
             Observable.FromEventPattern<EventHandler, EventArgs>(x => command.IsActiveChanged += x, x => command.IsActiveChanged -= x)
                .Select(x => command.IsActive)
                .Subscribe(x => IsBusy = x)
                .AddTo(Anchors);

            isExecuting
                .Subscribe(x => command.IsActive = x)
                .AddTo(Anchors);

            raiseCanExecuteChangedRequests
                .Subscribe(command.RaiseCanExecuteChanged)
                .AddTo(Anchors);
        }

        private CommandWrapper(ICommand command)
        {
             InnerCommand = command;
        }

        public bool IsBusy
        {
            get => isBusy;
            set => this.RaiseAndSetIfChanged(ref isBusy, value);
        }

        public string Error
        {
            get => error;
            private set => this.RaiseAndSetIfChanged(ref error, value);
        }

        public string Description
        {
            get => description;
            set => this.RaiseAndSetIfChanged(ref description, value);
        }

        private ICommand InnerCommand { get; }

        public bool CanExecute(object parameter)
        {
            return InnerCommand.CanExecute(parameter);
        }

        public void Execute(object parameter)
        {
            Error = null;
            try
            {
                isExecuting.OnNext(true);
                InnerCommand.Execute(parameter);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                isExecuting.OnNext(false);
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add => InnerCommand.CanExecuteChanged += value;
            remove => InnerCommand.CanExecuteChanged -= value;
        }

        public static CommandWrapper Create<T>(DelegateCommand<T> command)
        {
            return new CommandWrapper(command);
        }

        public static CommandWrapper Create(DelegateCommand command)
        {
            return new CommandWrapper(command);
        }

        public static CommandWrapper Create(Action execute)
        {
            return Create(new DelegateCommand(execute));
        }
        
        private static CommandWrapper FromReactiveCommand<T, TResult>(ReactiveCommand<T, TResult> command)
        {
            var result = new CommandWrapper(command);
            command.IsExecuting.Subscribe(x => result.IsBusy = x).AddTo(result.Anchors);
            command.ThrownExceptions.Subscribe(x => result.HandleException(x)).AddTo(result.Anchors);
            result.raiseCanExecuteChangedRequests
                .Subscribe(() => throw new NotSupportedException($"RaiseCanExecuteChanged is not supported for commands of type {command}"))
                .AddTo(result.Anchors);

            return result;
        }
        
        public static CommandWrapper Create(Func<Task> execute, IObservable<bool> canExecute)
        {
            return FromReactiveCommand(ReactiveCommand.CreateFromTask(execute, canExecute));
        }
        
        public static CommandWrapper Create(Func<Task> execute)
        {
            return Create(execute, Observable.Return(true).Concat(Observable.Never<bool>()));
        }
        
        public static CommandWrapper Create<TParam>(Func<TParam, Task> execute, IObservable<bool> canExecute)
        {
            return FromReactiveCommand(ReactiveCommand.CreateFromTask(execute, canExecute));
        }

        public static CommandWrapper Create<TParam>(Func<TParam, Task> execute)
        {
            return Create(execute, Observable.Return(true).Concat(Observable.Never<bool>()));
        }
        
        public static CommandWrapper Create(Action execute, Func<bool> canExecute)
        {
            return Create(new DelegateCommand(execute, canExecute));
        }
        
        public static CommandWrapper Create<T>(Action<T> execute, Func<T, bool> canExecute)
        {
            return Create(new DelegateCommand<T>(execute, canExecute));
        }
        
        public static CommandWrapper Create<T>(Action<T> execute)
        {
            return Create(new DelegateCommand<T>(execute));
        }

        public CommandWrapper RaiseCanExecuteChangedWhen(IObservable<Unit> eventSource)
        {
            Guard.ArgumentNotNull(() => eventSource);

            eventSource.Subscribe(RaiseCanExecuteChanged).AddTo(Anchors);
            return this;
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