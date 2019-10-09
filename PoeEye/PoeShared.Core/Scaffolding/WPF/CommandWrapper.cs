using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Input;
using Guards;
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
            InnerCommand = command;

             Observable.FromEventPattern<EventHandler, EventArgs>(x => command.IsActiveChanged += x, x => command.IsActiveChanged -= x)
                .Select(x => command.IsActive)
                .Subscribe(x => IsBusy = x)
                .AddTo(Anchors);

            isExecuting
                .Subscribe(x => command.IsActive = x)
                .AddTo(Anchors);

            raiseCanExecuteChangedRequests
                .Subscribe(() => command.RaiseCanExecuteChanged())
                .AddTo(Anchors);
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

        public static CommandWrapper Create(Action execute, Func<bool> canExecute)
        {
            return Create(new DelegateCommand(execute, canExecute));
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