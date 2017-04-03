using System;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Guards;
using JetBrains.Annotations;
using PoeShared.Prism;

namespace PoeShared.Modularity
{
    internal class SchedulerProvider : ISchedulerProvider
    {
        private readonly IScheduler uiScheduler;
        private readonly ConcurrentDictionary<string, IScheduler> schedulers = new ConcurrentDictionary<string, IScheduler>();

        public SchedulerProvider(
            [NotNull] [Microsoft.Practices.Unity.Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Microsoft.Practices.Unity.Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.uiScheduler = uiScheduler;
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            schedulers[WellKnownSchedulers.Background] = bgScheduler;
            schedulers[WellKnownSchedulers.UI] = uiScheduler;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IScheduler GetOrCreate(string name)
        {
            Log.Instance.Debug($"[SchedulerProvider.GetOrCreate '{name}'] Retrieving scheduler...");
            return schedulers.GetOrAdd(name, Create);
        }

        private IScheduler Create(string name)
        {
            Guard.ArgumentNotNull(name, nameof(name));

            Log.Instance.Debug($"[SchedulerProvider.Create '{name}'] Creating new dispatcher");
            var consumer = new TaskCompletionSource<IScheduler>();
            var dispatcherThread = new Thread(InitializeDispatcherThread)
            {
                Name = $"SC-{name}",
                IsBackground = true,
            };
            dispatcherThread.SetApartmentState(ApartmentState.STA);

            Log.Instance.Debug($"[SchedulerProvider.Create '{name}'] Thread started");

            dispatcherThread.Start(consumer);
            return consumer.Task.Result;
        }

        private void InitializeDispatcherThread(object arg)
        {
            var consumer = arg as TaskCompletionSource<IScheduler>;
            if (consumer != null)
            {
                InitializeDispatcherThread(consumer);
            }
            else
            {
                Log.Instance.Debug($"[SchedulerProvider.InitializeDispatcherThread] Wrong args: {arg}");
            }
        }

        private void InitializeDispatcherThread(TaskCompletionSource<IScheduler> consumer)
        {
            try
            {
                Log.Instance.Debug($"[SchedulerProvider.InitializeDispatcherThread] Thread started");
                var dispatcher = Dispatcher.CurrentDispatcher;
                Log.Instance.Debug($"[SchedulerProvider.InitializeDispatcherThread] Dispatcher: {dispatcher}");
                var scheduler = new DispatcherScheduler(dispatcher);
                Log.Instance.Debug($"[SchedulerProvider.InitializeDispatcherThread] Scheduler: {dispatcher}");
                consumer.TrySetResult(scheduler);

                Log.Instance.Debug($"[KeyboardEventsSource.InitializeKeyboardThread] Starting dispatcher...");
                Dispatcher.Run();
            }
            catch (Exception e)
            {
                Log.HandleException(e);
                consumer.TrySetException(e);
            }
            finally
            {
                Log.Instance.Debug($"[KeyboardEventsSource.InitializeKeyboardThread] Thread completed");
            }
        }
    }
}
