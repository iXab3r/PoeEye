namespace PoeEyeUi
{
    using System.Reflection;
    using System.Windows;

    using log4net;

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
        }
#if !DEBUG

        protected override void OnStartup(StartupEventArgs e, bool? isFirstInstance)
        {
            base.OnStartup(e, isFirstInstance);

            if (isFirstInstance == false)
            {
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

                Shutdown(1);
            }
        }
#endif
    }
}