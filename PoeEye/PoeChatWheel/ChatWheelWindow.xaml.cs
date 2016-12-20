using System;
using System.Collections.Generic;
using System.IO;
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
using PoeShared;
using PoeShared.Scaffolding;
using RadialMenu.Controls;
using ReactiveUI;
using Cursor = System.Windows.Input.Cursor;

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

            Log.Instance.Debug($"[PoeChatWheelWindow..ctor] Initializing chat wheel window...");

            chatWheel
                .WhenAnyValue(x => x.IsOpen)
                .Where(x => x)
                .ObserveOn(this)
                .Subscribe(
                    x =>
                    {
                        Log.Instance.Debug($"[PoeChatWheelWindow.CenterToMouse] Centering menu to mouse");
                        this.CenterToMouse();
                    })
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
                .Subscribe(SetCentralMenuItem)
                .AddTo(Anchors);
        }

        public RadialMenu.Controls.RadialMenu RadialMenu { get; set; }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowsServices.SetWindowExTransparent(hwnd);
        }

        private void SetCentralMenuItem(RadialMenuCentralItem centralItem)
        {
            Log.Instance.Debug($"[PoeChatWheelWindow.SetCentralItem] Setting central item to {centralItem}");
            RadialMenu.CentralItem = centralItem;
        }

        private void SetMenuItems(List<RadialMenuItem> items)
        {
            Log.Instance.Debug($"[PoeChatWheelWindow.SetMenuItems] Setting menu items (count: {items.Count})");

            RadialMenu.Items = items;
        }
    }
}
