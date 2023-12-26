using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using Polly;

namespace PoeShared.Services;

internal sealed class SingleInstanceValidationHelper : DisposableReactiveObject, ISingleInstanceValidationHelper
{
    private static readonly IFluentLog Log = typeof(SingleInstanceValidationHelper).PrepareLogger();

    public SingleInstanceValidationHelper(
        string mutexId,
        Application parent,
        IApplicationAccessor applicationAccessor
        )
    {
        MutexId = mutexId;
        applicationAccessor.WhenTerminate
            .Select(x => $"Terminate with code {x}").Subscribe(x =>
            {
                Log.Info($"Detected application termination with code {x}, disposing mutex to avoid leak");
                Dispose();
                Log.Info($"Processed application termination");
            })
            .AddTo(Anchors);
        AcquireMutexOrShutdown(MutexId, applicationAccessor, parent).AddTo(Anchors);
    }
    
    public string MutexId { get; }

    private static IDisposable AcquireMutexOrShutdown(
        string mutexId,
        IApplicationAccessor applicationAccessor,
        Application application)
    {
        var anchors = new CompositeDisposable();
        Log.Info($"Acquiring mutex {mutexId}...");
        var mutex = new Mutex(true, mutexId);

        var initialThread = Thread.CurrentThread;
        var threadDispatcher = Dispatcher.CurrentDispatcher;
        Log.Info($"Mutex will be disposed on dispatcher {threadDispatcher}, thread: { new { initialThread.Name, initialThread.ManagedThreadId } }");
        
        var mutexAcquired = 
            Policy.Handle<Exception>(ex =>
            {
                Log.Warn($"Exception occured when attempted to get mutex: {ex.Message}");
                return true;
            }).WaitAndRetry(new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(8),
                TimeSpan.FromSeconds(13)
            }).Execute(() =>
            {
                Log.Info($"Trying to acquire mutex {mutexId}");
                if (!mutex.WaitOne(TimeSpan.Zero, true))
                {
                    throw new InvalidStateException($"Failed to await mutex {mutexId}, another instance is running?");
                }

                return true;
            }); 

        if (mutexAcquired)
        {
            Log.Info($"Acquired mutex {mutexId}");

            var mutexReleaseAction = () =>
            {
                Log.Info($"Releasing mutex {mutexId}");
                mutex.ReleaseMutex();
                Log.Info("Released mutex");
            };
            
            Disposable.Create(() =>
            {
                if (Thread.CurrentThread != initialThread)
                {
                    Log.Info($"Dispatching mutex release action as current thread differs from initial");
                    threadDispatcher.Invoke(() =>
                    {
                        Log.Info($"Invoking mutex release action");
                        mutexReleaseAction();
                    });
                }
                else
                {
                    mutexReleaseAction();
                }
            }).AddTo(anchors);
        }
        else
        {
            Log.Warn($"Application is already running, mutex: {mutexId}");
            ShowShutdownWarning(applicationAccessor, application);
        }

        return anchors;
    }
    
    private static void ShowShutdownWarning(
        IApplicationAccessor applicationAccessor,
        Application application)
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName();
        var window = application.MainWindow;
        var title = $"{assemblyName.Name} v{assemblyName.Version}";
        var message = "Application is already running !";
        if (window != null)
        {
            MessageBox.Show(window, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        Log.Warn("Shutting down application");
        applicationAccessor.Terminate(-1);
    }
}