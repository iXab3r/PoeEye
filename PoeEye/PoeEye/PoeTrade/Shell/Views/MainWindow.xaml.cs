using System;
using System.Windows;

namespace PoeEye.PoeTrade.Shell.Views
{
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