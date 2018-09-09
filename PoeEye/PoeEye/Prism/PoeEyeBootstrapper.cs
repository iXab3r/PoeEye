using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using PoeEye.PoeTrade.Shell.ViewModels;
using PoeEye.PoeTrade.Shell.Views;
using PoeShared;
using PoeShared.Communications.Chromium;
using PoeShared.Scaffolding;
using Prism.Modularity;
using Prism.Unity;
using Unity;

namespace PoeEye.Prism
{
    internal sealed class PoeEyeBootstrapper : UnityBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell()
        {
            base.InitializeShell();

            Log.Instance.Info($"[Bootstrapper] Initializing shell...");
            var sw = Stopwatch.StartNew();

            Mouse.OverrideCursor = new Cursor(new MemoryStream(Properties.Resources.PathOfExile_102));
            var splashWindow = new SplashScreen("Resources\\Splash.png");
            splashWindow.Show(true, false);

            var window = (Window)Shell;
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            Application.Current.MainWindow = window;

            Observable
                .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => window.Loaded += h, h => window.Loaded -= h)
                .Take(1)
                .Subscribe(
                    () =>
                    {
                        sw.Stop();
                        Log.Instance.Info($"[Bootstrapper] Shell initialization has taken {sw.ElapsedMilliseconds}ms");
                    });
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new ConfigurationModuleCatalog();
        }

        public override void Run(bool runWithDefaultConfiguration)
        {
            base.Run(runWithDefaultConfiguration);

            var moduleCatalog = Container.Resolve<IModuleCatalog>();
            var modules = moduleCatalog.Modules.ToArray();
            Log.Instance.Info(
                $"Modules list:\n{modules.Select(x => new {x.ModuleName, x.ModuleType, x.State, x.InitializationMode, x.DependsOn}).DumpToTextRaw()}");

            var window = (Window)Shell;

            var viewModel = Container.Resolve<IMainWindowViewModel>();
            window.DataContext = viewModel;
            window.Show();
        }

        public void Dispose()
        {
            Log.Instance.Info($"Disposing Chromium...");
            var chromium = Container.Resolve<IChromiumBootstrapper>();
            chromium?.Dispose();
        }
    }
}