using System;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using JetBrains.Annotations;
using log4net;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;

namespace PoeShared.Modularity
{
    internal class SchedulerProvider : DisposableReactiveObject, ISchedulerProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SchedulerProvider));

        private readonly ConcurrentDictionary<string, IScheduler> schedulers = new ConcurrentDictionary<string, IScheduler>();
        private readonly IScheduler uiScheduler;

        public SchedulerProvider(
            [NotNull] [Dependency(WellKnownSchedulers.Background)]
            IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)]
            IScheduler uiScheduler)
        {
            this.uiScheduler = uiScheduler;
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            schedulers[WellKnownSchedulers.Background] = bgScheduler;
            schedulers[WellKnownSchedulers.UI] = uiScheduler;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public IScheduler GetOrCreate(string name)
        {
            Log.Debug($"[{name}] Retrieving scheduler...");
            return schedulers.GetOrAdd(name, Create);
        }

        private IScheduler Create(string name)
        {
            Guard.ArgumentNotNull(name, nameof(name));

            Log.Debug($"[{name}] Creating new dispatcher");
            var consumer = new TaskCompletionSource<IScheduler>();
            var dispatcherThread = new Thread(InitializeDispatcherThread)
            {
                Name = $"SC-{name}",
                IsBackground = true
            };
            dispatcherThread.SetApartmentState(ApartmentState.STA);

            Log.Debug($"[{name}] Thread started");

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
                Log.Debug($"Wrong args: {arg}");
            }
        }

        private void InitializeDispatcherThread(TaskCompletionSource<IScheduler> consumer)
        {
            try
            {
                Log.Debug("Thread started");
                var dispatcher = Dispatcher.CurrentDispatcher;
                Log.Debug($"Dispatcher: {dispatcher}");
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
                Log.Debug($"Scheduler: {dispatcher}");
                consumer.TrySetResult(scheduler);

                Log.Debug("Starting dispatcher...");
                Dispatcher.Run();
            }
            catch (Exception e)
            {
                Log.HandleException(e);
                consumer.TrySetException(e);
            }
            finally
            {
                Log.Debug("Thread completed");
            }
        }

        private void LogEvent(string eventName, DispatcherHookEventArgs eventArgs)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    $"[{eventName}] Priority: {eventArgs.Operation.Priority} Status: {eventArgs.Operation.Status}, Operation: {eventArgs.Operation.Task}");
            }
        }
    }
}