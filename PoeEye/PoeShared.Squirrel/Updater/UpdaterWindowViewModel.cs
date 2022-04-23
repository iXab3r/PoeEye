using System.Reactive.Disposables;
using System.Reactive.Linq;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PropertyBinder;
using ReactiveUI;
using Squirrel;
using System;
using System.Linq;
using PoeShared.Native;
using PoeShared.Services;
using PoeShared.UI;

namespace PoeShared.Squirrel.Updater;

internal sealed class UpdaterWindowViewModel : DisposableReactiveObject, IUpdaterWindowViewModel
{
    private static readonly IFluentLog Log = typeof(UpdaterWindowViewModel).PrepareLogger();

    private static readonly Binder<UpdaterWindowViewModel> Binder = new();

    static UpdaterWindowViewModel()
    {
        Binder
            .BindIf(x => (x.SelectedReleaseEntry == null || !x.ApplicationUpdater.AvailableReleases.Contains(x.SelectedReleaseEntry)) && x.ApplicationUpdater.AvailableReleases.Count > 0, x => x.ApplicationUpdater.AvailableReleases[0])
            .To(x => x.SelectedReleaseEntry);
            
        Binder.BindIf(x => x.CurrentVersionEntry == null && x.ApplicationUpdater.LatestUpdate != null && x.ApplicationUpdater.LatestUpdate.CurrentlyInstalledVersion != null, x => x.ApplicationUpdater.LatestUpdate.CurrentlyInstalledVersion)
            .To(x => x.CurrentVersionEntry);
            
        Binder.BindIf(x => x.ApplicationUpdater.AvailableReleases.Count > 0, x => x.ApplicationUpdater.AvailableReleases.OrderByDescending(y => y.Version).FirstOrDefault(y => x.CurrentVersionEntry == null || y.Version < x.CurrentVersionEntry.Version))
            .Else(x => default)
            .To(x => x.PreviousVersionEntry);
    }

    public UpdaterWindowViewModel(
        UpdaterWindowArgs args,
        IWindowViewController viewController,
        IAppArguments appArguments,
        IApplicationAccessor applicationAccessor,
        IUpdateSourceProvider updateSourceProvider,
        IApplicationUpdaterViewModel appUpdater,
        IErrorMonitorViewModel errorMonitor)
    {
        UpdateSourceProvider = updateSourceProvider;
        ErrorMonitor = errorMonitor;
        Disposable.Create(() => Log.Info("Disposing Updater view model")).AddTo(Anchors);
        ApplicationUpdater = appUpdater.AddTo(Anchors);
        Title = $"{appArguments.AppTitle} UPDATER";
        Message = args.Message;
        MessageLevel = args.MessageLevel;
        AllowTermination = args.AllowTermination;

        this.WhenAnyValue(x => x.SelectedReleaseEntry)
            .Where(x => x != null)
            .Subscribe(async x => await ApplicationUpdater.PrepareForceUpdate(x))
            .AddTo(Anchors);
            
        CloseCommand = CommandWrapper.Create(() => viewController.Close(true));
        TerminateCommand = CommandWrapper.Create(async () =>
        {
            Log.Debug("User decided to terminate application");
            applicationAccessor.Terminate(-1);
        });
        UpdateToVersionCommand = CommandWrapper.Create<IReleaseEntry>(x =>
        {
            ShowAdvanced = true;
            SelectedReleaseEntry = x;
            ApplicationUpdater.ApplyUpdateCommand.Execute(null);
        });
            
        Binder.Attach(this).AddTo(Anchors);
        Disposable.Create(() => Log.Info("Disposed")).AddTo(Anchors);
    }

    public IErrorMonitorViewModel ErrorMonitor { get; }

    public IUpdateSourceProvider UpdateSourceProvider { get; }

    public string Title { get; }

    public string Message { get; }
        
    public bool AllowTermination { get; }
        
    public FluentLogLevel MessageLevel { get; }
        
    public IApplicationUpdaterViewModel ApplicationUpdater { get; }

    public IReleaseEntry SelectedReleaseEntry { get; set; }
        
    public IReleaseEntry PreviousVersionEntry { get; private set; }
        
    public IReleaseEntry CurrentVersionEntry { get; private set; }
        
    public bool ShowAdvanced { get; set; }
        
    public CommandWrapper CloseCommand { get; }
        
    public CommandWrapper TerminateCommand { get; }
        
    public CommandWrapper UpdateToVersionCommand { get; }
}