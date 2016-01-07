namespace PoeEyeUi.PoeTrade.Views
{
    using System;
    using System.Windows;

    using MahApps.Metro.Controls;

    using Microsoft.Practices.Unity;

    using PoeEye.Prism;

    using PoeShared;
    using PoeShared.Prism;

    using PoeWhisperMonitor.Prism;

    using Prism;

    using ViewModels;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly UnityContainer unityContainer = new UnityContainer();

        private readonly IMainWindowViewModel mainWindowViewModel;

        public MainWindow()
        {
            InitializeComponent();

            Log.Instance.InfoFormat("Application started");
            unityContainer.AddExtension(new CommonRegistrations());
            unityContainer.AddExtension(new PoeWhisperRegistrations());
            unityContainer.AddExtension(new LiveRegistrations());
            unityContainer.AddExtension(new UiRegistrations());
            
            mainWindowViewModel = unityContainer.Resolve<IMainWindowViewModel>();
            DataContext = mainWindowViewModel;
            Application.Current.Exit += ApplicationOnExit;
        }

        private void ApplicationOnExit(object sender, ExitEventArgs exitEventArgs)
        {
            mainWindowViewModel?.Dispose();
        }
    }
}
