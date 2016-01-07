namespace PoeEyeUi
{
    using System;
    using System.Reflection;
    using System.Windows;

    using log4net;

    using PoeShared;

    using PoeTrade.Views;

    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public App()
        {
#if DEBUG
            GlobalContext.Properties["configuration"] = "Debug";
#else
            GlobalContext.Properties["configuration"] = "Release";
#endif

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        }


        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            Log.Instance.Error($"Unhandled application exception", unhandledExceptionEventArgs.ExceptionObject as Exception);
        }

        protected override void OnStartup(StartupEventArgs e, bool? isFirstInstance)
        {
            base.OnStartup(e, isFirstInstance);

            if (isFirstInstance == false)
            {
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
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            var mainWindow = new MainWindow();
            
            mainWindow.Show();
        }
    }
}