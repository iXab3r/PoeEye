using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using log4net;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Modularity
{
    public sealed class EnforcedThreadScheduler : IScheduler
    {
        private static readonly IFluentLog Log = typeof(EnforcedThreadScheduler).PrepareLogger();

        private readonly EventLoopScheduler threadScheduler;
        private Thread schedulerThread;
        
        public EnforcedThreadScheduler(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Log.Info($"Initializing new scheduler {name}");
            threadScheduler = new EventLoopScheduler(start =>
            {
                Log.Info($"Initializing thread for scheduler {name}");
                if (schedulerThread != null)
                {
                    throw new InvalidOperationException("This method must(and will in current implementation) be called only once");
                }
                schedulerThread = new Thread(start)
                {
                    Name = $"{name}",
                    IsBackground = true,
                    Priority = ThreadPriority.Normal
                };
                Log.Debug($"Setting apartment state for thread {schedulerThread.Name}");
                schedulerThread.SetApartmentState(ApartmentState.STA);
                Log.Info($"Scheduler {name} with thread {schedulerThread.Name} initialized");
                return schedulerThread;
            });
        }

        public bool IsOnSchedulerThread => Thread.CurrentThread == schedulerThread;

        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            if (IsOnSchedulerThread)
            {
                action(this, state);
                return Disposable.Empty;
            }

            return threadScheduler.Schedule(state, action);
        }

        public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            if (dueTime <= TimeSpan.Zero && IsOnSchedulerThread)
            {
                action(this, state);
                return Disposable.Empty;
            }
            return threadScheduler.Schedule(state, dueTime, action);
        }

        public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            if (dueTime <= Now && IsOnSchedulerThread)
            {
                action(this, state);
                return Disposable.Empty;
            }

            return threadScheduler.Schedule(state, dueTime, action);
        }

        public DateTimeOffset Now => threadScheduler.Now;
    }
}