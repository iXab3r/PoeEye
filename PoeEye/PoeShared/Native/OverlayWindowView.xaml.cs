using System;

namespace PoeShared.Native
{
    /// <summary>
    ///     Interaction logic for Window1.xaml
    /// </summary>
    public partial class OverlayWindowView
    {
        private readonly OverlayMode overlayMode;

        public OverlayWindowView(OverlayMode overlayMode = OverlayMode.Transparent)
        {
            this.overlayMode = overlayMode;
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            switch (overlayMode)
            {
                case OverlayMode.Layered:
                    MakeLayered();
                    break;
                default:
                    MakeTransparent();
                    break;
            }
        }
    }
}