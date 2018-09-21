using System;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeShared.Modularity
{
    internal class SchedulerProvider : DisposableReactiveObject, ISchedulerProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SchedulerProvider));
        
        private readonly ConcurrentDictionary<string, IScheduler> schedulers = new ConcurrentDictionary<string, IScheduler>();
        private readonly IScheduler uiScheduler;

        public SchedulerProvider(
            [NotNull] [Unity.Attributes.Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Unity.Attributes.Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
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
            Log.Debug($"[SchedulerProvider.GetOrCreate '{name}'] Retrieving scheduler...");
            return schedulers.GetOrAdd(name, Create);
        }

        private IScheduler Create(string name)
        {
            Guard.ArgumentNotNull(name, nameof(name));

            Log.Debug($"[SchedulerProvider.Create '{name}'] Creating new dispatcher");
            var consumer = new TaskCompletionSource<IScheduler>();
            var dispatcherThread = new Thread(InitializeDispatcherThread)
            {
                Name = $"SC-{name}",
                IsBackground = true
            };
            dispatcherThread.SetApartmentState(ApartmentState.STA);

            Log.Debug($"[SchedulerProvider.Create '{name}'] Thread started");

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
                Log.Debug($"[SchedulerProvider.InitializeDispatcherThread] Wrong args: {arg}");
            }
        }

        private void InitializeDispatcherThread(TaskCompletionSource<IScheduler> consumer)
        {
            try
            {
                Log.Debug($"[SchedulerProvider.InitializeDispatcherThread] Thread started");
                var dispatcher = Dispatcher.CurrentDispatcher;
                Log.Debug($"[SchedulerProvider.InitializeDispatcherThread] Dispatcher: {dispatcher}");
                var scheduler = new DispatcherScheduler(dispatcher);
                Observable
                    .FromEventPattern<DispatcherHookEventHandler, DispatcherHookEventArgs>(
                        h => scheduler.Dispatcher.Hooks.OperationStarted += h,
                        h => scheduler.Dispatcher.Hooks.OperationStarted -= h)
                    .Subscribe(eventArgs => LogEvent("OperationStarted", eventArgs.EventArgs))
                    .AddTo(Anchors);
                Observable
                    .FromEventPattern<DispatcherHookEventHandler, DispatcherHookEventArgs>(
                        h => scheduler.Dispatcher.Hooks.OperationPriorityChanged += h,
                        h => scheduler.Dispatcher.Hooks.OperationPriorityChanged -= h)
                    .Subscribe(eventArgs => LogEvent("OperationPriorityChanged", eventArgs.EventArgs))
                    .AddTo(Anchors);
                Observable
                    .FromEventPattern<DispatcherHookEventHandler, DispatcherHookEventArgs>(
                        h => scheduler.Dispatcher.Hooks.OperationAborted += h,
                        h => scheduler.Dispatcher.Hooks.OperationAborted -= h)
                    .Subscribe(eventArgs => LogEvent("OperationAborted", eventArgs.EventArgs))
                    .AddTo(Anchors);
                Observable
                    .FromEventPattern<DispatcherHookEventHandler, DispatcherHookEventArgs>(
                        h => scheduler.Dispatcher.Hooks.OperationPriorityChanged += h,
                        h => scheduler.Dispatcher.Hooks.OperationPriorityChanged -= h)
                    .Subscribe(eventArgs => LogEvent("OperationPriorityChanged", eventArgs.EventArgs))
                    .AddTo(Anchors);
                Observable
                    .FromEventPattern<DispatcherHookEventHandler, DispatcherHookEventArgs>(
                        h => scheduler.Dispatcher.Hooks.OperationPosted += h,
                        h => scheduler.Dispatcher.Hooks.OperationPosted -= h)
                    .Subscribe(eventArgs => LogEvent("OperationPosted", eventArgs.EventArgs))
                    .AddTo(Anchors);
                Log.Debug($"[SchedulerProvider.InitializeDispatcherThread] Scheduler: {dispatcher}");
                consumer.TrySetResult(scheduler);

                Log.Debug($"[KeyboardEventsSource.InitializeKeyboardThread] Starting dispatcher...");
                Dispatcher.Run();
            }
            catch (Exception e)
            {
                Log.HandleException(e);
                consumer.TrySetException(e);
            }
            finally
            {
                Log.Debug($"[KeyboardEventsSource.InitializeKeyboardThread] Thread completed");
            }
        }

        private void LogEvent(string eventName, DispatcherHookEventArgs eventArgs)
        {
            if (Log.IsTraceEnabled)
            {
                Log.Trace($"[{eventName}] Priority: {eventArgs.Operation.Priority} Status: {eventArgs.Operation.Status}, Operation: {eventArgs.Operation.Task}");
            }
        }
    }
}