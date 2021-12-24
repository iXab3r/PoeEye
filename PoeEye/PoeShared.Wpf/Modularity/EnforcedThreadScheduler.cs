using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Modularity
{
    public sealed class EnforcedThreadScheduler : IScheduler
    {
        private static readonly IFluentLog Log = typeof(EnforcedThreadScheduler).PrepareLogger();

        private readonly IScheduler threadScheduler;
        private Thread schedulerThread;

        public EnforcedThreadScheduler(Thread thread, IScheduler threadScheduler)
        {
            Log.Info($"Using existing thread scheduler: {threadScheduler}");
            this.threadScheduler = threadScheduler;
            this.schedulerThread = thread;
        }

        public EnforcedThreadScheduler(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
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
                Log.Debug(() => $"Setting apartment state for thread {schedulerThread.Name}");
                schedulerThread.SetApartmentState(ApartmentState.STA);
                Log.Info($"Scheduler {name} with thread {schedulerThread.Name} initialized");
                return schedulerThread;
            });
        }

        public string Name { get; }

        public bool IsOnSchedulerThread => Environment.CurrentManagedThreadId == schedulerThread?.ManagedThreadId;

        public DateTimeOffset Now => threadScheduler.Now;

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

        public override string ToString()
        {
            return $"EnforcedThreadScheduler {Name}({(schedulerThread == null ? "No thread yet" : $"with thread {schedulerThread.Name} (Id #{schedulerThread.ManagedThreadId})")})";
        }
    }
}