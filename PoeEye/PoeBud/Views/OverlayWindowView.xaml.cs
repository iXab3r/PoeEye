using System;
using System.Windows;

namespace PoeBud.Views
{
    /// <summary>
    ///     Interaction logic for Window1.xaml
    /// </summary>
    public partial class OverlayWindowView
    {
        public OverlayWindowView()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.MakeTransparent();
        }
    }
}