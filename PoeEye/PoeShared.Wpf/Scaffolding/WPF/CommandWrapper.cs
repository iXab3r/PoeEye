using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Input;
using PoeShared.Logging;
using PoeShared.Modularity;
using Prism.Commands;
using ReactiveUI;

namespace PoeShared.Scaffolding.WPF;

public sealed class CommandWrapper : DisposableReactiveObject, ICommand
{
    private static readonly IFluentLog Log = typeof(CommandWrapper).PrepareLogger();
    private static readonly IScheduler UIScheduler = SchedulerProvider.RedirectToUiScheduler;

    private readonly Subject<bool> isExecuting = new();
    private readonly Subject<Exception> thrownExceptions = new();
    private readonly Subject<Unit> raiseCanExecuteChangedRequests = new();
    
    private CommandWrapper(DelegateCommandBase command)
    {
        Guard.ArgumentNotNull(command, nameof(command));

        InnerCommand = command;
        Observable.FromEventPattern<EventHandler, EventArgs>(x => command.IsActiveChanged += x, x => command.IsActiveChanged -= x)
            .Select(x => command.IsActive)
            .ObserveOn(UIScheduler)
            .SubscribeSafe(x => IsBusy = x, Log.HandleUiException)
            .AddTo(Anchors);

        isExecuting
            .ObserveOn(UIScheduler)
            .SubscribeSafe(x => command.IsActive = x, Log.HandleUiException)
            .AddTo(Anchors);

        raiseCanExecuteChangedRequests
            .ObserveOn(UIScheduler)
            .SubscribeSafe(x => command.RaiseCanExecuteChanged(), Log.HandleUiException)
            .AddTo(Anchors);
    }
        
    public IObservable<Exception> ThrownExceptions => thrownExceptions;

    private CommandWrapper(ICommand command)
    {
        InnerCommand = command;
    }

    public bool IsBusy { get; set; }

    public string Error { get; private set; }

    public string Description { get; set; }

    private ICommand InnerCommand { get; }

    public bool CanExecute(object parameter)
    {
        try
        {
            return InnerCommand.CanExecute(parameter);
        }
        catch (InvalidCastException)
        {
            return false;
        }
        catch (Exception e)
        {
            throw new ApplicationException($"CommandWrapper.CanExecute exception, parameter: {parameter}", e);
        }
    }

    public void Execute(object parameter)
    {
        ResetError();
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

    public void ResetError()
    {
        Error = null;
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
        command.IsExecuting.SubscribeSafe(x => result.IsBusy = x, Log.HandleUiException).AddTo(result.Anchors);
        command.ThrownExceptions.SubscribeSafe(result.HandleException, Log.HandleUiException).AddTo(result.Anchors);
        result.raiseCanExecuteChangedRequests
            .SubscribeSafe(() => Log.Warn($"RaiseCanExecuteChanged is not supported for commands of type {command}"), Log.HandleUiException)
            .AddTo(result.Anchors);

        return result;
    }
        
    public static CommandWrapper Create(Action execute, IObservable<bool> canExecute)
    {
        return FromReactiveCommand(ReactiveCommand.Create(execute, canExecute.ObserveOn(UIScheduler)));
    }

    public static CommandWrapper Create(Func<Task> execute, IObservable<bool> canExecute)
    {
        return FromReactiveCommand(ReactiveCommand.CreateFromTask(execute, canExecute.ObserveOn(UIScheduler)));
    }

    public static CommandWrapper Create(Func<Task> execute)
    {
        return Create(execute, Observable.Return(true).Concat(Observable.Never<bool>()));
    }

    public static CommandWrapper Create<TParam>(Func<TParam, Task> execute, IObservable<bool> canExecute)
    {
        return FromReactiveCommand(ReactiveCommand.CreateFromTask(execute, canExecute.ObserveOn(UIScheduler)));
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
    
    public static CommandWrapper Create<T>(Action<T> execute, IObservable<bool> raiseCanExecuteWhen)
    {
        return Create<T>(async x => execute(x), raiseCanExecuteWhen);
    }

    public CommandWrapper RaiseCanExecuteChangedWhen<T>(IObservable<T> eventSource)
    {
        Guard.ArgumentNotNull(() => eventSource);

        return RaiseCanExecuteChangedWhen(eventSource.ToUnit());
    }

    private CommandWrapper RaiseCanExecuteChangedWhen(IObservable<Unit> eventSource)
    {
        Guard.ArgumentNotNull(() => eventSource);

        eventSource
            .SubscribeSafe(RaiseCanExecuteChanged, Log.HandleUiException).AddTo(Anchors);
        return this;
    }

    private void HandleException(Exception exception)
    {
        Log.HandleException(exception);
        Error = exception.Message;
    }

    public void RaiseCanExecuteChanged()
    {
        raiseCanExecuteChangedRequests.OnNext(Unit.Default);
    }
}