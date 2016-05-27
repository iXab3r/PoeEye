using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using PoePricer;

namespace PoePickitTestApp
{
    public partial class TooltipWindow
    {
        public TooltipWindow()
        {
            InitializeComponent();
        }

        public void SetTooltip(PoeToolTip toolTip)
        {
            if (toolTip == null)
            {
                Hide();
                return;
            }

            Show();
            Left = Control.MousePosition.X;
            Top = Control.MousePosition.Y;

            FontSize = toolTip.FontSize;
            Background = new SolidColorBrush(toolTip.BackColor);
            Foreground = new SolidColorBrush(toolTip.TextColor);

            LeftLabel.Content = toolTip.ArgText;
            RightLabel.Content = toolTip.ValueText;
        }
    }
}