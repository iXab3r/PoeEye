using System;
using System.Drawing;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Gma.System.MouseKeyHook;
using PoePickitTestApp.Extensions;
using PoePricer;
using ReactiveUI;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using MouseEventHandler = System.Windows.Forms.MouseEventHandler;

namespace PoePickitTestApp
{
    public partial class TooltipWindow
    {
        private static readonly CompositeDisposable Anchors = new CompositeDisposable();

        public TooltipWindow()
        {
            InitializeComponent();

            var globalEvents = Hook.GlobalEvents();
            globalEvents.AddTo(Anchors);

            Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
                h => globalEvents.MouseMove += h,
                h => globalEvents.MouseMove -= h)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Where(x => Visibility == Visibility.Visible)
                .Subscribe(_ => RefreshPosition())
                .AddTo(Anchors);
        }

        public void SetTooltip(PoeToolTip toolTip)
        {
            if (toolTip == null)
            {
                Hide();
                return;
            }
            RefreshPosition();
            Show();

         
            FontSize = toolTip.FontSize;
            Background = new SolidColorBrush(toolTip.BackColor);
            LeftLabel.Foreground = RightLabel.Foreground = new SolidColorBrush(toolTip.TextColor);

            


            LeftLabel.Content = toolTip.ArgText;
            RightLabel.Content = toolTip.ValueText;
        }

        private void RefreshPosition()
        {
            var cursorSize = System.Windows.Forms.Cursor.Current?.Size;
            Left = Control.MousePosition.X;
            Top = Control.MousePosition.Y + (cursorSize?.Height ?? 0);
        }
    }
}