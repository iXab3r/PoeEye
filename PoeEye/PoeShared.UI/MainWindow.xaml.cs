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
using System.Windows.Navigation;
using System.Windows.Shapes;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;

namespace PoeShared.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }

    internal sealed class MainWindowViewModel : DisposableReactiveObject
    {
        public MainWindowViewModel(IKeyboardEventsSource eventsSource)
        {
            EventsSource = eventsSource;
            LongCommand = CommandWrapper.Create(async () =>
            {
                await Task.Delay(3000);
            });
            
            ErrorCommand = CommandWrapper.Create(async () =>
            {
                await Task.Delay(3000);
                throw new ApplicationException("Error");
            });
        }
        
        public IKeyboardEventsSource EventsSource { get; }

        public CommandWrapper LongCommand { get; }
        
        public CommandWrapper ErrorCommand { get; }
    }
}