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
using PoePricer.Extensions;


namespace PoePricer
{
    

    public partial class TtForm : Form
    {

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;





        public const int WM_CLIPBOARDUPDATE = 0x031D;
        public static IntPtr HWND_MESSAGE = new IntPtr(-3);

        // See http://msdn.microsoft.com/en-us/library/ms632599%28VS.85%29.aspx#message_only
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        // See http://msdn.microsoft.com/en-us/library/ms633541%28v=vs.85%29.aspx
        // See http://msdn.microsoft.com/en-us/library/ms649033%28VS.85%29.aspx
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);



        public TtForm()
        {
            InitializeComponent();
            

        }

        public void la()
        {

            
        }

        public void SetLeftText(string text)
        {
            this.LabelLeft.Text = text;
        }

        public void SetRightText(string text)
        {
            this.LabelRight.Text = text;
        }

        public void SetLocation(int coorX = 1000, int coorY = 0)
        {
            this.Location = new Point(coorX, coorY);
        }

    

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            
            if (m.Msg == WM_CLIPBOARDUPDATE)
            {
                SetRightText("ddddddddddd");
                Refresh();
                

            }
        }

        private void TtForm_Load(object sender, EventArgs e)
        {
            AddClipboardFormatListener(this.Handle);
        }
    }
}
