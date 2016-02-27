namespace PoeEyeUi
{
    using System;
    using System.Windows;

    using log4net;

    using Microsoft.Practices.Unity;

    using NBug.Core.Submission.Web;
    using NBug.Enums;

    using PoeEye.Prism;

    using PoeShared;
    using PoeShared.Prism;

    using PoeTrade.ViewModels;
    using PoeTrade.Views;

    using PoeWhisperMonitor.Prism;

    using Prism;

    using ReactiveUI;

    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private static readonly Lazy<UnityContainer> UnityContainerInstance = new Lazy<UnityContainer>();

        public static IUnityContainer Container => UnityContainerInstance.Value;

        public App()
        {
            // used for log4net configuration
#if DEBUG
            GlobalContext.Properties["configuration"] = "Debug";
#else
            GlobalContext.Properties["configuration"] = "Release";
#endif
            Log.Instance.Info("Application started");

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            Log.Instance.Debug("Initializing DI container...");
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