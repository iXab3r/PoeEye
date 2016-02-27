namespace PoeEyeUi
{
    using System;
    using System.Windows;

    using log4net;

    using Microsoft.Practices.Unity;

    using PoeEye.Prism;

    using PoeShared;
    using PoeShared.Prism;

    using PoeTrade.ViewModels;
    using PoeTrade.Views;

    using PoeWhisperMonitor.Prism;

    using Prism;

    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private static readonly Lazy<UnityContainer> UnityContainerInstance = new Lazy<UnityContainer>();

        public static IUnityContainer Container => UnityContainerInstance.Value;

        public App()
        {
            // Uncomment the following after testing to see that NBug is working as configured
            // NBug.Settings.ReleaseMode = true;
            // todo: add other configuration options here

            AppDomain.CurrentDomain.UnhandledException += NBug.Handler.UnhandledException;
            Application.Current.DispatcherUnhandledException += NBug.Handler.DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            Log.Instance.InfoFormat("Application started");
            Container.AddExtension(new CommonRegistrations());
            Container.AddExtension(new PoeWhisperRegistrations());
            Container.AddExtension(new LiveRegistrations());
            Container.AddExtension(new UiRegistrations());
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            Log.Instance.Error($"Unhandled application exception", unhandledExceptionEventArgs.ExceptionObject as Exception);
        }

        protected override void OnStartup(StartupEventArgs e, bool? isFirstInstance)
        {
            base.OnStartup(e, isFirstInstance);

            if (isFirstInstance != false)
            {
                return;
            }

            Log.Instance.Warn($"Application is already running !");
#if !DEBUG
            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            var window = MainWindow;
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
            Log.Instance.Warn($"Shutting down...");
            Shutdown(1);
#endif
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            var mainWindow = Container.Resolve<MainWindow>();
            mainWindow.DataContext = Container.Resolve<IMainWindowViewModel>();

            mainWindow.Show();
        }
    }
}