using System;
using System.Reactive.Concurrency;
using System.Threading;
using System.Windows.Threading;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Modularity;

/// <summary>
/// Represents an object that schedules units of work on a <see cref="System.Windows.Threading.Dispatcher"/>.
/// </summary>
/// <remarks>
/// This scheduler type is typically used indirectly through the <see cref="DispatcherObservable.ObserveOnDispatcher{TSource}(IObservable{TSource})"/> and <see cref="DispatcherObservable.SubscribeOnDispatcher{TSource}(IObservable{TSource})"/> methods that use the Dispatcher on the calling thread.
/// </remarks>
public sealed class DispatcherScheduler : LocalScheduler, ISchedulerPeriodic, IDispatcherScheduler
{
    private static readonly IFluentLog Log = typeof(DispatcherScheduler).PrepareLogger();

    /// <summary>
    /// Gets the scheduler that schedules work on the <see cref="System.Windows.Threading.Dispatcher"/> for the current thread.
    /// </summary>
    public static DispatcherScheduler Current
    {
        get
        {
            var dispatcher = System.Windows.Threading.Dispatcher.FromThread(Thread.CurrentThread);
            if (dispatcher == null)
            {
                throw new InvalidOperationException("NO_DISPATCHER_CURRENT_THREAD");
            }

            return new DispatcherScheduler(dispatcher);
        }
    }

    public static void EnsureExists()
    {
        var currentDispatcher = Current;//will throw if not exists
    }

    /// <summary>
    /// Constructs a <see cref="DispatcherScheduler"/> that schedules units of work on the given <see cref="System.Windows.Threading.Dispatcher"/>.
    /// </summary>
    /// <param name="dispatcher"><see cref="DispatcherScheduler"/> to schedule work on.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dispatcher"/> is <c>null</c>.</exception>
    public DispatcherScheduler(System.Windows.Threading.Dispatcher dispatcher)
    {
        Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        Priority = System.Windows.Threading.DispatcherPriority.Normal;

    }

    /// <summary>
    /// Constructs a <see cref="DispatcherScheduler"/> that schedules units of work on the given <see cref="System.Windows.Threading.Dispatcher"/> at the given priority.
    /// </summary>
    /// <param name="dispatcher"><see cref="DispatcherScheduler"/> to schedule work on.</param>
    /// <param name="priority">Priority at which units of work are scheduled.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dispatcher"/> is <c>null</c>.</exception>
    public DispatcherScheduler(System.Windows.Threading.Dispatcher dispatcher, System.Windows.Threading.DispatcherPriority priority)
    {
        Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        Priority = priority;
    }
    
    public static DispatcherScheduler CreateDispatcherScheduler(string name, ThreadPriority priority)
    {
        return CreateDispatcherSchedulerInternal(name, priority);
    }
    
    private static DispatcherScheduler CreateDispatcherSchedulerInternal(string name, ThreadPriority priority)
    {
        Guard.ArgumentNotNull(name, nameof(name));

        Log.WithSuffix(name).Debug($"Creating new dispatcher");
        var consumer = new TaskCompletionSource<DispatcherScheduler>();
        var dispatcherThread = new Thread(InitializeDispatcherThread)
        {
            Name = $"S#{name}",
            Priority = priority,
            IsBackground = true
        };
        dispatcherThread.SetApartmentState(ApartmentState.STA);
        Log.WithSuffix(name).Debug($"Starting dispatcher thread");
        dispatcherThread.Start(consumer);
        Log.WithSuffix(name).Debug($"Dispatcher thread started");
        return consumer.Task.Result;
    }

    private static void InitializeDispatcherThread(object arg)
    {
        if (arg is not TaskCompletionSource<DispatcherScheduler> consumer)
        {
            throw new InvalidOperationException($"Wrong args: {arg}");
        }

        RunDispatcherThread(consumer);
    }

    private static void RunDispatcherThread(TaskCompletionSource<DispatcherScheduler> consumer)
    {
        try
        {
            Log.Debug("Dispatcher thread started");
            var dispatcher = Dispatcher.CurrentDispatcher;
            Log.Debug($"Dispatcher: {dispatcher}");
            var scheduler = new DispatcherScheduler(dispatcher); 
            //using var listener = Listen(scheduler);
          
            Log.Debug($"Scheduler: {dispatcher}");
            consumer.TrySetResult(scheduler);

            Log.Debug("Starting dispatcher...");
            Dispatcher.Run();
        }
        catch (Exception e)
        {
            Log.HandleException(e);
            consumer.TrySetException(e);
            throw; 
        }
        finally
        {
            Log.Debug("Dispatcher thread completed");
        }
    }

    /// <summary>
    /// Gets the <see cref="System.Windows.Threading.Dispatcher"/> associated with the <see cref="DispatcherScheduler"/>.
    /// </summary>
    public System.Windows.Threading.Dispatcher Dispatcher { get; }

    /// <summary>
    /// Gets the priority at which work items will be dispatched.
    /// </summary>
    public System.Windows.Threading.DispatcherPriority Priority { get; }

    /// <summary>
    /// Schedules an action to be executed on the dispatcher.
    /// </summary>
    /// <typeparam name="TState">The type of the state passed to the scheduled action.</typeparam>
    /// <param name="state">State passed to the action to be executed.</param>
    /// <param name="action">Action to be executed.</param>
    /// <returns>The disposable object used to cancel the scheduled action (best effort).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public override IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var d = new SingleAssignmentDisposable();

        Dispatcher.BeginInvoke(
            new Action(() =>
            {
                if (!d.IsDisposed)
                {
                    d.Disposable = action(this, state);
                }
            }),
            Priority
        );

        return d;
    }

    /// <summary>
    /// Schedules an action to be executed after <paramref name="dueTime"/> on the dispatcher, using a <see cref="System.Windows.Threading.DispatcherTimer"/> object.
    /// </summary>
    /// <typeparam name="TState">The type of the state passed to the scheduled action.</typeparam>
    /// <param name="state">State passed to the action to be executed.</param>
    /// <param name="action">Action to be executed.</param>
    /// <param name="dueTime">Relative time after which to execute the action.</param>
    /// <returns>The disposable object used to cancel the scheduled action (best effort).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public override IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var dt = Scheduler.Normalize(dueTime);
        if (dt.Ticks == 0)
        {
            return Schedule(state, action);
        }

        return ScheduleSlow(state, dt, action);
    }

    private IDisposable ScheduleSlow<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
    {
        var d = new MultipleAssignmentDisposable();

        var timer = new System.Windows.Threading.DispatcherTimer(Priority, Dispatcher);

        timer.Tick += (s, e) =>
        {
            var t = Interlocked.Exchange(ref timer, null);
            if (t != null)
            {
                try
                {
                    d.Disposable = action(this, state);
                }
                finally
                {
                    t.Stop();
                    action = static (s, t) => Disposable.Empty;
                }
            }
        };

        timer.Interval = dueTime;
        timer.Start();

        d.Disposable = Disposable.Create(() =>
        {
            var t = Interlocked.Exchange(ref timer, null);
            if (t != null)
            {
                t.Stop();
                action = static (s, t) => Disposable.Empty;
            }
        });

        return d;
    }

    /// <summary>
    /// Schedules a periodic piece of work on the dispatcher, using a <see cref="System.Windows.Threading.DispatcherTimer"/> object.
    /// </summary>
    /// <typeparam name="TState">The type of the state passed to the scheduled action.</typeparam>
    /// <param name="state">Initial state passed to the action upon the first iteration.</param>
    /// <param name="period">Period for running the work periodically.</param>
    /// <param name="action">Action to be executed, potentially updating the state.</param>
    /// <returns>The disposable object used to cancel the scheduled recurring action (best effort).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="period"/> is less than <see cref="TimeSpan.Zero"/>.</exception>
    public IDisposable SchedulePeriodic<TState>(TState state, TimeSpan period, Func<TState, TState> action)
    {
        if (period < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(period));
        }

        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var timer = new System.Windows.Threading.DispatcherTimer(Priority, Dispatcher);

        var state1 = state;

        timer.Tick += (s, e) =>
        {
            state1 = action(state1);
        };

        timer.Interval = period;
        timer.Start();

        return Disposable.Create(() =>
        {
            var t = Interlocked.Exchange(ref timer, null);
            if (t != null)
            {
                t.Stop();
                action = static _ => _;
            }
        });
    }

    public bool CheckAccess()
    {
        return Dispatcher.CheckAccess();
    }
}
