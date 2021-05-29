using System;
using System.Windows;
using Unity;

namespace PoeShared.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : ApplicationBase
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            var window = new MainWindow();
            window.DataContext = Container.Resolve<MainWindowViewModel>();
            window.Show();
        }
    }
}