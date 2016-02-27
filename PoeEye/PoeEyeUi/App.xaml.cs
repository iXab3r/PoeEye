namespace PoeEyeUi
{
    using System;
    using System.Reflection;
    using System.Windows;

    using Exceptionless;
    using Exceptionless.Models;

    using log4net;

    using Microsoft.Practices.Unity;

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
        private static readonly string AppVersion = $"v{Assembly.GetExecutingAssembly().GetName().Version}";
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

            Log.Instance.Debug("Initializing exceptionless...");
            ExceptionlessClient.Default.Configuration.ApiKey = "dkjcxnVxQO9Nx6zJdYYyAW66gHt5YP5XCmHNmjYj";
            ExceptionlessClient.Default.Configuration.DefaultTags.Add($".NET {Environment.Version}");
            ExceptionlessClient.Default.Configuration.DefaultTags.Add($"OS:{Environment.OSVersion}");
            ExceptionlessClient.Default.Configuration.DefaultTags.Add(AppVersion);
            ExceptionlessClient.Default.Configuration.IncludePrivateInformation = true;
            ExceptionlessClient.Default.Configuration.SetVersion(AppVersion);
            ExceptionlessClient.Default.Configuration.SetUserIdentity($"{Environment.UserName}@{Environment.MachineName}");

            ExceptionlessClient.Default.SubmitEvent(new Event { Message = AppVersion, Type = "Version" });
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
            var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
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