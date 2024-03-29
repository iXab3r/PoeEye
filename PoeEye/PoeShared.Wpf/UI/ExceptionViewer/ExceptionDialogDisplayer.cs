using System;
using System.Reactive.Disposables;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Services;

namespace PoeShared.UI;

internal sealed class ExceptionDialogDisplayer : DisposableReactiveObject, IExceptionDialogDisplayer
{
    private static readonly IFluentLog Log = typeof(ExceptionDialogDisplayer).PrepareLogger();
    private readonly IAppArguments appArguments;
    private readonly IReportItemsAggregator reportItemsAggregator;
    private readonly IFactory<ExceptionDialogViewModel, ICloseController, IReportItemsAggregator> dialogViewModelFactory;
    private readonly SerialDisposable activeWindowAnchors = new();
    private readonly NamedLock dialogLock = new(nameof(ExceptionDialogDisplayer));

    public ExceptionDialogDisplayer(
        IAppArguments appArguments,
        IReportItemsAggregator reportItemsAggregator,
        IFactory<ExceptionDialogViewModel, ICloseController, IReportItemsAggregator> dialogViewModelFactory)
    {
        activeWindowAnchors.AddTo(Anchors);
        this.appArguments = appArguments;
        this.reportItemsAggregator = reportItemsAggregator;
        this.dialogViewModelFactory = dialogViewModelFactory;
    }

    public void ShowDialog(ExceptionDialogConfig config)
    {
        try
        {
            ShowExceptionViewer(config);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to show ExceptionViewer, falling back to MessageBox", e);
            try
            {
                ShowMessageBox(config);
            }
            catch (Exception e1)
            {
                Log.Error($"Failed to show MessageBox with exception", e1);
            }
        }
    }

    private void ShowMessageBox(ExceptionDialogConfig config)
    {
        Log.Debug($"Showing MessageBox, exception: {config.Exception}, config: {config}");

        Window owner = null;
        var appWindow = Application.Current.MainWindow;
        if (appWindow != null && !(appWindow is ExceptionDialogView) && appWindow.IsLoaded && appWindow.IsVisible)
        {
            owner = appWindow;
        }

        var content = config.Exception.Message;

        if (owner != null)
        {
            System.Windows.MessageBox.Show(
                owner, 
                content, 
                config.Title, 
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        else
        {
            System.Windows.MessageBox.Show(
                config.Exception.Message, 
                config.Title, 
                MessageBoxButton.OK,
                MessageBoxImage.Error,
                MessageBoxResult.OK, 
                MessageBoxOptions.DefaultDesktopOnly);
        }
    }

    private void ShowExceptionViewer(ExceptionDialogConfig config)
    {
        try
        {
            var appDispatcher = Application.Current?.Dispatcher;
            if (appDispatcher != null && !appDispatcher.CheckAccess())
            {
                Log.Warn("Exception occurred on non-UI thread, rescheduling to UI");
                appDispatcher.Invoke(() => ShowExceptionViewer(config), DispatcherPriority.Send);
                Log.Debug($"Sent signal to UI thread to report crash related to exception {config.Exception.Message}");
                return;
            }

            Log.Info($"Acquiring dialog lock");
            using var @lock = dialogLock.Enter();
            Log.Info($"Showing custom ExceptionViewer, exception: {config.Exception}, config: {new { config.Title, config.AppName, config.Timestamp, config.Exception }}");

            var windowAnchors = new CompositeDisposable();
            Window window = null;
            try
            {
                window = new ExceptionDialogView();
                var appWindow = Application.Current?.MainWindow;
                if (appWindow != null && appWindow.IsLoaded && appWindow.IsVisible)
                {
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.Owner = appWindow;
                }
                else
                {
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
            
                Disposable.Create(
                        () =>
                        {
                            Log.Debug($"Closing ExceptionViewer, value: {config.Exception}");
                            window.Close();
                        })
                    .AddTo(windowAnchors);
            
                var closeController = new CloseController(windowAnchors.Dispose);

                var dialogViewModel = dialogViewModelFactory.Create(closeController, reportItemsAggregator);
                dialogViewModel.Config = config;
                window.DataContext = dialogViewModel;
                window.ListenWhenLoaded()
                    .Subscribe(x =>
                    {
                        window.Activate();
                    })
                    .AddTo(windowAnchors);
                window.ShowDialog();
            }
            catch (Exception e)
            {
                Log.HandleException(new ApplicationException("Exception in ExceptionReporter :-(", e));
                window?.Close();
                throw;
            }
        }
        catch (Exception e)
        {
            Log.HandleException(new ApplicationException("Failed to show exception viewer :-(", e));
            throw;
        }
    }
}