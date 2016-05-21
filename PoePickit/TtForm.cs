using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Threading;
using PoePickit.Extensions;
using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;


namespace PoePickit
{
    public partial class TtForm : Form
    {
        public TtForm()
        {
            InitializeComponent();
        }

        public void Initialize(PoeToolTip toolTip)
        {
            if (toolTip == null)
            {
                Hide();
            }
            else
            {
                //set tooltip location
                var point = new Point();
                if ((Size.Width/2 + MousePosition.X) > Screen.GetWorkingArea(MousePosition).Width)
                    point.X = Screen.GetWorkingArea(MousePosition).Width - Size.Width/2;
                else
                    point.X = MousePosition.X - Size.Width/2;

                if ((Size.Height + 40 + MousePosition.Y) > Screen.GetWorkingArea(MousePosition).Height)
                    point.Y = MousePosition.Y - Size.Height - 40;
                else
                    point.Y = MousePosition.Y + 40;
                Location = point;


                BackColor = toolTip.BackColor;
                SetText(toolTip.ArgText, toolTip.ValueText);
                Show();
            }
        }

        
        private void SetText(string leftText, string rightText)
        {
            this.LabelLeft.Text = leftText;
            this.LabelRight.Text = rightText;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            ShowInTaskbar = true; // Remove from taskbar.
            Hide();
        }
    }
}
