using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using log4net;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.UI
{
    public partial class FakeDelayControl : UserControl
    {
        private static readonly IFluentLog Log = typeof(FakeDelayControl).PrepareLogger();

        public static readonly DependencyProperty RenderDelayProperty = DependencyProperty.Register(
            "RenderDelay", typeof(int), typeof(FakeDelayControl), new PropertyMetadata(default(int)));

        public static readonly DependencyProperty LoadDelayProperty = DependencyProperty.Register(
            "LoadDelay", typeof(int), typeof(FakeDelayControl), new PropertyMetadata(5000));

        public FakeDelayControl()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        public int RenderDelay
        {
            get { return (int)GetValue(RenderDelayProperty); }
            set { SetValue(RenderDelayProperty, value); }
        }

        public int LoadDelay
        {
            get { return (int)GetValue(LoadDelayProperty); }
            set { SetValue(LoadDelayProperty, value); }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (LoadDelay <= 0)
            {
                return;
            }
            Log.Warn($"Starting fake OnLoaded delay {LoadDelay}");
            Thread.Sleep(LoadDelay);
            Log.Warn("Fake OnLoaded delay ended");
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (RenderDelay <= 0)
            {
                return;
            }
            Log.Warn($"Starting fake render delay {RenderDelay}");
            Thread.Sleep(RenderDelay);
            Log.Warn("Fake delay render ended");
        }
    }
}