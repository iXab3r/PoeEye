namespace PoeEyeUi.PoeTrade.Views
{
    using System;

    using MahApps.Metro.Controls;

    using Microsoft.Practices.Unity;

    using PoeEye.Prism;

    using PoeShared;
    using PoeShared.Prism;

    using Prism;

    using ViewModels;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private UnityContainer unityContainer = new UnityContainer();

        public MainWindow()
        {
            InitializeComponent();

            Log.Instance.InfoFormat("Application started");
            unityContainer.AddExtension(new CommonRegistrations());
            unityContainer.AddExtension(new LiveRegistrations());
            unityContainer.AddExtension(new UiRegistrations());

            DataContext = unityContainer.Resolve<MainWindowViewModel>();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            Log.Instance.Error($"Unhandled application exception", unhandledExceptionEventArgs.ExceptionObject as Exception);
        }
    }
}
