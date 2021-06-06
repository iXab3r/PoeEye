using System.Threading;
using System.Windows;
using System.Windows.Controls;
using log4net;

namespace PoeShared.UI
{
    public partial class FakeDelayControl : UserControl
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FakeDelayControl));

        public FakeDelayControl()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Log.Warn("Starting fake delay");
            Thread.Sleep(30000);
            Log.Warn("Fake delay ended");
        }
    }
}