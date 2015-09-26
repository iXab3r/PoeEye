using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PoeEyeUi
{
    using MahApps.Metro.Controls;

    using Microsoft.Practices.Unity;

    using PoeEye.Prism;

    using PoeShared;
    using PoeShared.Prism;

    using Prism;

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
        }
    }
}
