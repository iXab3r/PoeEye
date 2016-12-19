using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Guards;
using PoeChatWheel.Utilities;
using PoeChatWheel.ViewModels;
using PoeShared.Scaffolding;
using RadialMenu.Controls;
using ReactiveUI;

namespace PoeChatWheel
{
    /// <summary>
    /// Interaction logic for ChatWheelWindow.xaml
    /// </summary>
    public partial class ChatWheelWindow : Window
    {
        private static readonly CompositeDisposable Anchors = new CompositeDisposable();

        public ChatWheelWindow(IPoeChatWheelViewModel chatWheel)
        {
            Guard.ArgumentNotNull(() => chatWheel);

            InitializeComponent();
            this.DataContext = chatWheel;

            chatWheel
                .WhenAnyValue(x => x.IsOpen)
                .Where(x => x)
                .ObserveOn(this)
                .Subscribe(x => this.CenterToMouse())
                .AddTo(Anchors);

            chatWheel.Items.Changed
                .Where(x => RadialMenu != null)
                .ObserveOn(this)
                .Select(x => chatWheel.Items.ToList())
                .Subscribe(SetMenuItems)
                .AddTo(Anchors);

            chatWheel.WhenAnyValue(x => x.CentralItem)
                .Where(x => RadialMenu != null)
                .ObserveOn(this)
                .Subscribe(x => RadialMenu.CentralItem = x)
                .AddTo(Anchors);

            this.IsVisibleChanged += OnIsVisibleChanged;
        }

        public RadialMenu.Controls.RadialMenu RadialMenu { get; set; }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowsServices.SetWindowExTransparent(hwnd);
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (!this.IsVisible)
            {
                return;
            }

            this.CenterToMouse();
        }

        private void SetMenuItems(List<RadialMenuItem> items)
        {
            RadialMenu.Items = items;
            RadialMenu.InvalidateArrange();
        }
    }
}
