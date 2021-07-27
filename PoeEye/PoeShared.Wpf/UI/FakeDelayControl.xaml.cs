using System.Threading;
using System.Windows;
using System.Windows.Controls;
using log4net;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.UI
{
    public partial class FakeDelayControl : UserControl
    {
        private static readonly IFluentLog Log = typeof(FakeDelayControl).PrepareLogger();

        public FakeDelayControl()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Log.Warn("Starting fake delay");
            Thread.Sleep(25000);
            Log.Warn("Fake delay ended");
        }
    }
}