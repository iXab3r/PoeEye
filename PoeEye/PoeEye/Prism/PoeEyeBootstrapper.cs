using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Common.Logging;
using PoeEye.Properties;
using PoeEye.Shell.Services;
using PoeEye.Shell.ViewModels;
using PoeEye.Shell.Views;
using PoeShared.Chromium.Communications;
using PoeShared.Native;
using PoeShared.Scaffolding;
using Prism.Modularity;
using Prism.Unity;
using Unity;

namespace PoeEye.Prism
{
    internal sealed class PoeEyeBootstrapper : UnityBootstrapper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeEyeBootstrapper));
        
        private readonly CompositeDisposable anchors = new CompositeDisposable();

        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell()
        {
            base.InitializeShell();

            Log.Info("Initializing shell...");
            var sw = Stopwatch.StartNew();

            Mouse.OverrideCursor = new Cursor(new MemoryStream(Resources.PathOfExile_102));
            

            var window = (Window)Shell;

            var splashWindow = new SplashWindowDisplayer(window);
            
            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => window.ContentRendered += h, h => window.ContentRendered -= h)
                .Take(1)
                .Subscribe(
                    () =>
                    {
                        Log.Debug($"Window rendered");
                        Application.Current.MainWindow = window;
                        splashWindow.Close();
                        Log.Info($"Window+Shell initialization has taken {sw.ElapsedMilliseconds}ms");
                    }).AddTo(anchors);
            
            Observable
                .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => window.Loaded += h, h => window.Loaded -= h)
                .Take(1)
                .Subscribe(
                    () =>
                    {
                        Log.Debug($"Window loaded");
                        Log.Info($"Shell initialization has taken {sw.ElapsedMilliseconds}ms");
                    }).AddTo(anchors);
            
            Log.Info($"Loading main window...");
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            Application.Current.MainWindow = window;
            
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            splashWindow.Show();
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new ConfigurationModuleCatalog();
        }

        public override void Run(bool runWithDefaultConfiguration)
        {
            base.Run(runWithDefaultConfiguration);
            
            var moduleManager = Container.Resolve<IModuleManager>();
            Observable
                .FromEventPattern<LoadModuleCompletedEventArgs>(h =>  moduleManager.LoadModuleCompleted += h, h => moduleManager.LoadModuleCompleted -= h)
                .Select(x => x.EventArgs)
                .Subscribe(
                    evt =>
                    {
                        if (evt.Error != null)
                        {
                            Log.Error($"[#{evt.ModuleInfo.ModuleName}] Error during loading occured, isHandled: {evt.IsErrorHandled}", evt.Error);
                        }
                        Log.Info($"[#{evt.ModuleInfo.ModuleName}] Module loaded");
                    }).AddTo(anchors);

            var moduleCatalog = Container.Resolve<IModuleCatalog>();
            var modules = moduleCatalog.Modules.ToArray();
            Log.Debug(
                $"Modules list:\n\t{modules.Select(x => new {x.ModuleName, x.ModuleType, x.State, x.InitializationMode, x.DependsOn}).DumpToTable()}");

            var window = (Window)Shell;

            var viewModel = Container.Resolve<IMainWindowViewModel>();
            window.DataContext = viewModel;
            viewModel.AddTo(anchors);
        }

        public void Dispose()
        {
            Log.Info("Disposing Chromium...");
            var chromium = Container.Resolve<IChromiumBootstrapper>();
            chromium?.Dispose();
            
            anchors.Dispose();
        }
    }
}