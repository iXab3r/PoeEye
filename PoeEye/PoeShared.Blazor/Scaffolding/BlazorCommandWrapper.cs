using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows.Input;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PoeShared.Services;
using ReactiveUI;
using Task = System.Threading.Tasks.Task;

namespace PoeShared.Blazor.Scaffolding;

public sealed class BlazorCommandWrapper
{
    private static readonly IFluentLog Log = typeof(BlazorCommandWrapper).PrepareLogger();

    [ThreadStatic] private static IScheduler currentScheduler;
    private static IScheduler CurrentScheduler => currentScheduler ??= CreateSynchronizationContextScheduler();

    private static IScheduler CreateSynchronizationContextScheduler()
    {
        var synchronizationContext = SynchronizationContext.Current;
        if (synchronizationContext == null)
        {
            throw new ArgumentException("Synchronization context must be set for Commands to work properly");
        }
        return new SynchronizationContextScheduler(synchronizationContext);
    }

    public static BlazorCommandWrapper<Unit, Unit> Create(Action execute)
    {
        var command = ReactiveCommand.Create(execute);
        return FromReactiveCommand(command);
    }
        
    public static BlazorCommandWrapper<Unit, Unit> Create()
    {
        return Create(() => { });
    }
    
    public static BlazorCommandWrapper<Unit, Unit> Create(Action execute, IObservable<bool> canExecute)
    {
        return FromReactiveCommand(ReactiveCommand.Create(execute, canExecute.ObserveOn(CurrentScheduler), CurrentScheduler));
    }

    public static BlazorCommandWrapper<Unit, Unit> Create(Func<Task> execute, IObservable<bool> canExecute)
    {
        return FromReactiveCommand(ReactiveCommand.CreateFromTask(execute, canExecute.ObserveOn(CurrentScheduler), CurrentScheduler));
    }
    
    public static BlazorCommandWrapper<TIn, Unit> Create<TIn>(Func<TIn, Task> execute, IObservable<bool> canExecute)
    {
        return FromReactiveCommand(ReactiveCommand.CreateFromTask(execute, canExecute.ObserveOn(CurrentScheduler), CurrentScheduler));
    }

    public static BlazorCommandWrapper<TIn, Unit> Create<TIn>(Action<TIn> execute)
    {
        return FromReactiveCommand(ReactiveCommand.Create(execute, outputScheduler: CurrentScheduler));
    }

    public static BlazorCommandWrapper<Unit, Unit> Create(Func<Task> execute)
    {
        return Create(execute, Observable.Return(true).Concat(Observable.Never<bool>()));
    }

    public static BlazorCommandWrapper<TIn, Unit> Create<TIn>(Func<TIn, Task> execute)
    {
        return Create(execute, Observable.Return(true).Concat(Observable.Never<bool>()));
    }

    public static BlazorCommandWrapper<TIn, Unit> Create<TIn>(Action<TIn> execute, IObservable<bool> raiseCanExecuteWhen)
    {
        return Create<TIn>(async x => execute(x), raiseCanExecuteWhen);
    }

    public static BlazorCommandWrapper<TIn, TOut> FromReactiveCommand<TIn, TOut>(ReactiveCommand<TIn, TOut> command)
    {
        var result = new BlazorCommandWrapper<TIn, TOut>(command);
        return result;
    }
}

/// <summary>
/// FIXME: This is almost a duplicate of CommandWrapper, will work only for WPF (but does not require UseWpf=true)
/// Next step - create commandwrapper implementation which will cover all cases - WPF, blazor-in-wpf, blazor-in-web, blazor-wasm
/// </summary>
public sealed class BlazorCommandWrapper<TParam, TResult> : DisposableReactiveObject, ICommandWrapper
{
     private static readonly IFluentLog Log = typeof(BlazorCommandWrapper<TParam, TResult>).PrepareLogger();

    private readonly Subject<Exception> thrownExceptions = new();
    private readonly ISharedResourceLatch isBusyLatch;
    private readonly ISubject<object> whenExecuted = new Subject<object>();
    private readonly IScheduler scheduler;
    private readonly int schedulerThreadId;

#if DEBUG
    // ReSharper disable once UnusedMember.Local Needed for debugging
    private string createdFrom = new StackTrace().ToString();
#endif

    internal BlazorCommandWrapper(ReactiveCommand<TParam, TResult> command)
    {
        command.ThrownExceptions.SubscribeSafe(HandleException, Log.HandleUiException).AddTo(Anchors);
        isBusyLatch = new SharedResourceLatch().AddTo(Anchors);
        isBusyLatch.WhenAnyValue(x => x.IsBusy).SubscribeSafe(x => IsBusy = x, Log.HandleUiException).AddTo(Anchors);
        
        schedulerThreadId = Environment.CurrentManagedThreadId;
        InnerCommand = command;//.AddTo(Anchors); Update to https://github.com/reactiveui/ReactiveUI/commit/d24e69f2dd47729045507d7f16ad0bb418c6925e
        Observable.FromEventPattern(
                handler => WpfCommand.CanExecuteChanged += handler,
                handler => WpfCommand.CanExecuteChanged -= handler)
            .Subscribe(_ => InnerCommandOnCanExecuteChanged(WpfCommand, EventArgs.Empty))
            .AddTo(Anchors); 
    }

    private void InnerCommandOnCanExecuteChanged(object sender, EventArgs e)
    {
#if WINDOWS // this code makes sense only for Windows / WPF
        if (Environment.CurrentManagedThreadId == schedulerThreadId)
        {
            return;
        }
        Log.Error($"CanExecute is executed on an invalid thread, expected thread: {schedulerThreadId}, actual: {Thread.CurrentThread} id: {Environment.CurrentManagedThreadId}");
#endif
    }
        
    public IObservable<Exception> ThrownExceptions => thrownExceptions;

    public bool IsBusy { get; private set; }

    public string Error { get; private set; }

    public string Description { get; set; }

    private ReactiveCommand<TParam, TResult> InnerCommand { get; }
    
    private ICommand WpfCommand => InnerCommand;

    public IObservable<object> WhenExecuted => whenExecuted;

    public bool CanExecute(object parameter)
    {
        try
        {
            return WpfCommand.CanExecute(parameter);
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

    public async Task ExecuteAsync(TParam parameter)
    {
        using var isBusy = isBusyLatch.Rent();
        
        ResetError();
        EnsureNotDisposed();

        await InnerCommand.Execute(parameter);
        whenExecuted.OnNext(parameter);
    }

    public void Execute(object parameter)
    {
        using var isBusy = isBusyLatch.Rent();

        ResetError();
        try
        {
            EnsureNotDisposed();
            
            WpfCommand.Execute(parameter is TParam param ? param : default);
            whenExecuted.OnNext(parameter);
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
    }

    public Task ExecuteAsync(object parameter = null)
    {
        return ExecuteAsync(parameter is TParam param ? param : default);
    }

    public void ResetError()
    {
        Error = null;
    }

    public event EventHandler CanExecuteChanged
    {
        add => WpfCommand.CanExecuteChanged += value;
        remove => WpfCommand.CanExecuteChanged -= value;
    }

    private void HandleException(Exception exception)
    {
        Log.HandleException(exception);
        Error = exception.Message;
        thrownExceptions.OnNext(exception);
    }
}

