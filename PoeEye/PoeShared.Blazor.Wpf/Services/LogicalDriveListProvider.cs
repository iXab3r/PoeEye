using System;
using System.IO;
using System.Management;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Services;

internal sealed class LogicalDriveListProvider : LazyReactiveObject<LogicalDriveListProvider>
{
    private static readonly IFluentLog Log = typeof(LogicalDriveListProvider).PrepareLogger();

    private readonly SourceCache<string, string> drives = new(x => x);

    public LogicalDriveListProvider()
    {
        Drives = drives.Connect().RemoveKey().Transform(x => new DirectoryInfo(x)).AsObservableList().AddTo(Anchors);
        
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
                Log.Error("Failed to stop Wql watcher", e);
            }
        }).AddTo(Anchors);
        Observable.FromEventPattern<EventArrivedEventHandler, EventArrivedEventArgs>(
                handler => watcher.EventArrived += handler,
                handler => watcher.EventArrived -= handler)
            .StartWithDefault()
            .Subscribe(_ => UpdateDrivesSafe())
            .AddTo(Anchors);
        
        watcher.Start();
    }
    
    public IObservableList<DirectoryInfo> Drives { get; }

    private void UpdateDrivesSafe()
    {
        try
        {
            Log.Info($"List of logical drives has changed, requesting updated list, current: {drives.Items.DumpToString()}");
            var currentDrives = Directory.GetLogicalDrives();
            drives.EditDiff(currentDrives);
            Log.Info($"List of logical drives has been updated to: {drives.Items.DumpToString()}");
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to update list of logical drives, current: {drives.Items.DumpToString()}", e);
        }
    }
}