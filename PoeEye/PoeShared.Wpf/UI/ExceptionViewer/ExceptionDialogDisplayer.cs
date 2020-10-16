using System;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using log4net;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI;

namespace PoeShared.Wpf.UI.ExceptionViewer
{
    internal sealed class ExceptionDialogDisplayer : DisposableReactiveObject, IExceptionDialogDisplayer
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ExceptionDialogDisplayer));
        private readonly IAppArguments appArguments;
        private readonly IFactory<ExceptionDialogViewModel, ICloseController> dialogViewModelFactory;
        private readonly SerialDisposable activeWindowAnchors = new SerialDisposable();

        public ExceptionDialogDisplayer(
            IAppArguments appArguments,
            IFactory<ExceptionDialogViewModel, ICloseController> dialogViewModelFactory)
        {
            activeWindowAnchors.AddTo(Anchors);
            this.appArguments = appArguments;
            this.dialogViewModelFactory = dialogViewModelFactory;
        }

        public void ShowDialog(ExceptionDialogConfig config, Exception exception)
        {
            Guard.ArgumentNotNull(exception, nameof(exception));

            try
            {
                ShowExceptionViewer(config, exception);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to show ExceptionViewer, falling back to MessageBox", e);
                try
                {
                    ShowMessageBox(config, exception);
                }
                catch (Exception e1)
                {
                    Log.Error($"Failed to show MessageBox with exception", e1);
                }
            }
        }

        public void ShowDialogAndTerminate(Exception exception)
        {
            
            var appDispatcher = Application.Current?.Dispatcher;
            if (appDispatcher != null && Dispatcher.CurrentDispatcher != appDispatcher)
            {
                Log.Warn("Exception occurred on non-UI thread, rescheduling to UI");
                appDispatcher.BeginInvoke(new Action(() => ShowDialogAndTerminate(exception)), DispatcherPriority.Send);
                Log.Debug($"Sent signal to UI thread to report crash related to exception {exception.Message}");
                return;
            }
            
            try
            {
                var config = new ExceptionDialogConfig
                {
                    AppName = appArguments.AppName,
                    Title = $"{appArguments.AppName} Error Report"
                };

                var configurationFilesToInclude = Directory
                    .EnumerateFiles(appArguments.AppDataDirectory, "*.cfg", SearchOption.TopDirectoryOnly);

                var logFilesToInclude = new DirectoryInfo(appArguments.AppDataDirectory)
                    .GetFiles("*.log", SearchOption.AllDirectories)
                    .OrderByDescending(x => x.LastWriteTime)
                    .Take(2)
                    .Select(x => x.FullName)
                    .ToArray();

                config.FilesToAttach = new[]
                    {
                        logFilesToInclude,
                        configurationFilesToInclude
                    }.SelectMany(x => x)
                    .ToArray();
                ShowDialog(config, exception);
                
                Log.Warn($"Forcefully terminating Environment due to unrecoverable error");
                Environment.Exit(-1);
            }
            catch (Exception e)
            {
                Log.HandleException(new ApplicationException("Exception in ExceptionReporter :-(", e));
            }
        }

        private void ShowMessageBox(ExceptionDialogConfig config, Exception exception)
        {
            Log.Debug($"Showing MessageBox, exception: {exception}, config: {config.DumpToTextRaw()}");

            Window owner = null;
            var appWindow = Application.Current.MainWindow;
            if (appWindow != null && !(appWindow is ExceptionDialogView) && appWindow.IsLoaded && appWindow.IsVisible)
            {
                owner = appWindow;
            }

            var content = exception.Message.ToString();

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
                    exception.Message, 
                    config.Title, 
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    MessageBoxResult.OK, 
                    MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        private void ShowExceptionViewer(ExceptionDialogConfig config, Exception exception)
        {
            Log.Debug($"Showing custom ExceptionViewer, exception: {exception}, config: {config.DumpToTextRaw()}");

            var windowAnchors = new CompositeDisposable();
            Window window = null;
            try
            {
                window = new ExceptionDialogView();
                var appWindow = Application.Current.MainWindow;
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
                            Log.Debug($"Closing ExceptionViewer, value: {exception}");
                            window.Close();
                        })
                    .AddTo(windowAnchors);
            
                var closeController = new CloseController(windowAnchors.Dispose);

                var dialogViewModel = dialogViewModelFactory.Create(closeController);
                dialogViewModel.Config = config;
                dialogViewModel.ExceptionSource = exception;
                window.DataContext = dialogViewModel;
                window.ShowDialog();
            }
            catch (Exception e)
            {
                window?.Close();
                throw;
            }
        }
    }
}