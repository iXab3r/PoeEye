using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Services;

internal sealed class LogicalDriveListProvider : LazyReactiveObject<LogicalDriveListProvider>
{
    private static readonly IFluentLog Log = typeof(LogicalDriveListProvider).PrepareLogger();
    private static readonly TimeSpan UpdateDrivesPeriod = TimeSpan.FromSeconds(60);

    private readonly SourceCache<string, string> drives = new(x => x);

    public LogicalDriveListProvider()
    {
        Drives = drives.Connect().RemoveKey().Transform(x => new DirectoryInfo(x)).AsObservableList().AddTo(Anchors);

        InitializeSafe();
    }

    public IObservableList<DirectoryInfo> Drives { get; }

    private void InitializeSafe()
    {
        try
        {
            UpdateDrivesSafe(); //run first check upon construction to get the initial list
        }
        catch (Exception e)
        {
            Log.Warn("Failed to retrieve initial logical drives list", e);
        }
        
        try
        {
            ListenForLogicalDrivesListUsingWmi()
                .Do(_ => Log.Info("Got the signal from WMI that list of drives has changed"))
                .Subscribe(UpdateDrivesSafe)
                .AddTo(Anchors);
            return;
        }
        catch (Exception e)
        {
            Log.Warn("Failed to initialize WMI-way of retrieving logical drives list, falling back to polling method", e);
        }

        try
        {
            Log.Info("Initializing listening for list of logical drives using polling method");

            Observable.Interval(UpdateDrivesPeriod, Scheduler.Default)
                .Subscribe(UpdateDrivesSafe)
                .AddTo(Anchors);
        }
        catch (Exception e)
        {
            Log.Warn("Failed to initialize polling method of retrieving logical drives list, list of drives will be empty", e);
        }
    }

    private static IObservable<Unit> ListenForLogicalDrivesListUsingWmi()
    {
        return Observable.Create<Unit>(observer =>
        {
            Log.Info("Initializing WMI-watcher listening for list of logical drives");
            var anchors = new CompositeDisposable();
            var query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent");
            var watcher = new ManagementEventWatcher(query);
            Disposable.Create(() =>
            {
                try
                {
                    watcher.Stop();
                }
                catch (Exception e)
                {
                    Log.Warn("Failed to stop WMI watcher", e);
                }
            }).AddTo(anchors);
            Observable.FromEventPattern<EventArrivedEventHandler, EventArrivedEventArgs>(
                    handler => watcher.EventArrived += handler,
                    handler => watcher.EventArrived -= handler, Scheduler.Default)
                .Subscribe(_ => observer.OnNext(Unit.Default))
                .AddTo(anchors);

            Log.Info("Starting WMI-watcher listening for list of logical drives");
            watcher.Start();
            return anchors;
        });
    }

    private void UpdateDrivesSafe()
    {
        try
        {
            var previousDrives = drives.Items.ToArray();
            var currentDrives = Directory.GetLogicalDrives();

            var newItems = currentDrives.Except(previousDrives).ToArray();
            var removedItems = previousDrives.Except(currentDrives).ToArray();

            if (newItems.IsEmpty() && removedItems.IsEmpty())
            {
                return;
            }

            Log.Info($"List of logical drives has changed(removed: {removedItems.DumpToString()}, added: {newItems.DumpToString()}), current: {previousDrives.DumpToString()}");
            drives.EditDiff(currentDrives);
            Log.Info($"List of logical drives has been updated to: {currentDrives.DumpToString()}");
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to update list of logical drives, current: {drives.Items.DumpToString()}", e);
        }
    }
}