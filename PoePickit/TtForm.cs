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


        public void SetText(string leftText, string rightText)
        {
            this.LabelLeft.Text = leftText;
            this.LabelRight.Text = rightText;
        }

        public void SetLocation(int coorX = 1000, int coorY = 0)
        {
            this.Location = new Point(coorX, coorY);
        }

        public void TtShow(string ArgText, string ValueText)
        {
            SetText(ArgText, ValueText);
            SetLocation(Cursor.Position.X, Cursor.Position.Y);
            this.Show();
            Refresh();


        }

        public void TtHide()
        {
            this.Hide();


        }


        /*   protected override void WndProc(ref Message m)
           {
               base.WndProc(ref m);


               if (m.Msg == WM_CLIPBOARDUPDATE)
               {

               }
           }
   */
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            ShowInTaskbar = true; // Remove from taskbar.
            this.TtHide();

         
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }


    }
}
