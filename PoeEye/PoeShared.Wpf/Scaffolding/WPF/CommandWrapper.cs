using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using PoeShared.Logging;
using PoeShared.Modularity;
using Prism.Commands;
using ReactiveUI;

namespace PoeShared.Scaffolding.WPF;

public sealed class CommandWrapper : DisposableReactiveObject, ICommand
{
    private static readonly IFluentLog Log = typeof(CommandWrapper).PrepareLogger();

    private readonly Subject<bool> isExecuting = new();
    private readonly Subject<Exception> thrownExceptions = new();
    private readonly Subject<Unit> raiseCanExecuteChangedRequests = new();
    private readonly ISubject<object> whenExecuted = new Subject<object>();
    private readonly DispatcherScheduler scheduler;

    private CommandWrapper(ICommand command)
    {
        scheduler = DispatcherScheduler.Current;
        InnerCommand = command;
        InnerCommand.CanExecuteChanged += InnerCommandOnCanExecuteChanged;
    }

    private void InnerCommandOnCanExecuteChanged(object sender, EventArgs e)
    {
        var actualScheduler = DispatcherScheduler.Current;
        if (actualScheduler != null && actualScheduler.Dispatcher.Thread == scheduler.Dispatcher.Thread)
        {
            return;
        }

        Log.Error($"CanExecute is executed on an invalid thread, expected: {scheduler}, actual: {actualScheduler}");
        if (Debugger.IsAttached)
        {
            Debugger.Break();
        }
    }

    public static CommandWrapper FromDelegateCommand(DelegateCommandBase command)
    {
        return FromDelegateCommand(command, DispatcherScheduler.Current);
    }
    
    public static CommandWrapper FromDelegateCommand(DelegateCommandBase command, IScheduler scheduler)
    {
        var result = new CommandWrapper(command);
        
        Observable.FromEventPattern<EventHandler, EventArgs>(x => command.IsActiveChanged += x, x => command.IsActiveChanged -= x)
            .Select(x => command.IsActive)
            .ObserveOn(scheduler)
            .SubscribeSafe(x => result.IsBusy = x, Log.HandleUiException)
            .AddTo(result.Anchors);

        result.isExecuting
            .ObserveOn(scheduler)
            .SubscribeSafe(x => command.IsActive = x, Log.HandleUiException)
            .AddTo(result.Anchors);

        result.raiseCanExecuteChangedRequests
            .ObserveOn(scheduler)
            .SubscribeSafe(x => command.RaiseCanExecuteChanged(), Log.HandleUiException)
            .AddTo(result.Anchors);

        return result;
    }
    
    public static CommandWrapper FromReactiveCommand<T, TResult>(ReactiveCommand<T, TResult> command)
    {
        var result = new CommandWrapper(command);
        command.IsExecuting.SubscribeSafe(x => result.IsBusy = x, Log.HandleUiException).AddTo(result.Anchors);
        command.ThrownExceptions.SubscribeSafe(result.HandleException, Log.HandleUiException).AddTo(result.Anchors);
        result.raiseCanExecuteChangedRequests
            .SubscribeSafe(() => Log.Warn($"RaiseCanExecuteChanged is not supported for commands of type {command}"), Log.HandleUiException)
            .AddTo(result.Anchors);

        return result;
    }
        
    public IObservable<Exception> ThrownExceptions => thrownExceptions;

    public bool IsBusy { get; set; }

    public string Error { get; private set; }

    public string Description { get; set; }

    private ICommand InnerCommand { get; }

    public IObservable<object> WhenExecuted => whenExecuted;

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
            whenExecuted.OnNext(parameter);
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
        return FromDelegateCommand(command);
    }

    public static CommandWrapper Create(DelegateCommand command)
    {
        return FromDelegateCommand(command);
    }

    public static CommandWrapper Create(Action execute)
    {
        return FromDelegateCommand(new DelegateCommand(execute));
    }
        
    public static CommandWrapper Create()
    {
        return Create(() => { });
    }
    
    public static CommandWrapper Create(Action execute, IObservable<bool> canExecute)
    {
        return FromReactiveCommand(ReactiveCommand.Create(execute, canExecute.ObserveOn(DispatcherScheduler.Current), DispatcherScheduler.Current));
    }

    public static CommandWrapper Create(Func<Task> execute, IObservable<bool> canExecute)
    {
        return FromReactiveCommand(ReactiveCommand.CreateFromTask(execute, canExecute.ObserveOn(DispatcherScheduler.Current), DispatcherScheduler.Current));
    }
    
    public static CommandWrapper Create<TParam>(Func<TParam, Task> execute, IObservable<bool> canExecute)
    {
        return FromReactiveCommand(ReactiveCommand.CreateFromTask(execute, canExecute.ObserveOn(DispatcherScheduler.Current), DispatcherScheduler.Current));
    }

    public static CommandWrapper Create(Func<Task> execute)
    {
        return Create(execute, Observable.Return(true).Concat(Observable.Never<bool>()));
    }

    public static CommandWrapper Create<TParam>(Func<TParam, Task> execute)
    {
        return Create(execute, Observable.Return(true).Concat(Observable.Never<bool>()));
    }

    public static CommandWrapper Create(Action execute, Func<bool> canExecute)
    {
        return FromDelegateCommand(new DelegateCommand(execute, canExecute));
    }

    public static CommandWrapper Create<T>(Action<T> execute, Func<T, bool> canExecute)
    {
        return FromDelegateCommand(new DelegateCommand<T>(execute, canExecute));
    }

    public static CommandWrapper Create<T>(Action<T> execute)
    {
        return FromReactiveCommand(ReactiveCommand.Create(execute));
    }
    
    public static CommandWrapper Create<T>(Action<T> execute, IObservable<bool> raiseCanExecuteWhen)
    {
        return Create<T>(async x => execute(x), raiseCanExecuteWhen);
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