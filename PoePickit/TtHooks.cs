using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using Gma.System.MouseKeyHook;
using PoePickit.Extensions;

namespace PoePickit
{
    public class TtHooks
    {
        public bool ttActive = false;

        public TtForm Forma = new TtForm();
        private readonly ItemParse ItemParser = new ItemParse();
        private ToolTip _tt = new ToolTip();
        

        public TtHooks()
        {
            // Note: for the application hook, use the Hook.AppEvents() instead
            m_GlobalHook = Hook.GlobalEvents();

            m_GlobalHook.MouseMoveExt += M_GlobalHook_MouseMoveExt;
            m_GlobalHook.KeyDown += M_GlobalHook_KeyDown;
            m_GlobalHook.KeyUp += M_GlobalHook_KeyUp;
            m_GlobalHook.KeyPress += M_GlobalHook_KeyPress;

        }


        private void M_GlobalHook_KeyPress(object sender, KeyPressEventArgs e)
        {
            
            if (e.KeyChar == 163)
            {
                Console.WriteLine("Space press");

            }
        }

        private void M_GlobalHook_MouseMoveExt(object sender, MouseEventExtArgs e)
        {

            

        }

        private void M_GlobalHook_KeyDown(object sender, KeyEventArgs e)
        {
            //control = 162
            if (e.KeyValue == 162)
            {
                var ttThread = new Thread(this.OnClipboardUpdate);
                ttThread.Start();
            }

        }

        private void M_GlobalHook_KeyUp(object sender, KeyEventArgs e)
        {
            Console.WriteLine("Key up");
            if (e.KeyValue == 163)
            {
                Console.WriteLine("Space up");

            }
        }




        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private static IKeyboardMouseEvents m_GlobalHook;

        private readonly ItemParse _parser = new ItemParse();


        public static IntPtr HwndMessage = new IntPtr(-3);

        private static string _lastItemClipboard;


     

        public void DisposeHooks()
        {
            m_GlobalHook.MouseMoveExt -= M_GlobalHook_MouseMoveExt;
            m_GlobalHook.KeyDown -= M_GlobalHook_KeyDown;
            m_GlobalHook.KeyUp -= M_GlobalHook_KeyUp;
            m_GlobalHook.KeyPress -= M_GlobalHook_KeyPress;
            //It is recommened to dispose it
            m_GlobalHook.Dispose();
        }


        private string GetActiveWindow()
        {
            const int nChars = 256;
            var buff = new StringBuilder(nChars);

            var handle = GetForegroundWindow();

            if (GetWindowText(handle, buff, nChars) > 0)
            {
                return buff.ToString();
            }
            return null;
        }

        private bool PoeNotActive()
        {
            if (GetActiveWindow() == "Path of Exile")
            {
                return false;
            }
            return true;
        }

        public void OnClipboardUpdate()
        {
            //if (PoeNotActive())
            //return;
            //проверка на текст

            /*if (string.IsNullOrEmpty(Clipboard.GetText()))
            {
                return;
            }*/


            Clipboard.GetText().DumpToConsole();
            var clip = Clipboard.GetText();
            
            if (Forma.Visible)
                if (clip == _lastItemClipboard)
                    return;

            _tt = ItemParser.CreateTooltip(clip);

            if (_tt == null)
            {
                Forma.TtHide();
                return;
            }
                
            _lastItemClipboard = clip;
            
            Forma.TtShow(_tt.ArgText, _tt.ValueText);


        }
    }
}