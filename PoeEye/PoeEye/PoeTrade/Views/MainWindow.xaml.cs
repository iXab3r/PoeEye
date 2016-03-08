namespace PoeEye.PoeTrade.Views
{
    using System;
    using System.Windows;

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            Application.Current.Exit += ApplicationOnExit;
        }

        private void ApplicationOnExit(object sender, ExitEventArgs exitEventArgs)
        {
            (DataContext as IDisposable)?.Dispose();
        }
    }
}