namespace WpfClipboardMonitor
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    /// <summary>
    /// Provides notifications when the contents of the clipboard is updated.
    /// </summary>
    public sealed class ClipboardNotifications
    {
        /// <summary>
        /// Occurs when the contents of the clipboard is updated.
        /// </summary>
        public static event EventHandler ClipboardUpdate = delegate { }; 

        private readonly static NotificationForm ClipboardForm = new NotificationForm();

        /// <summary>
        /// Raises the <see cref="ClipboardUpdate"/> event.
        /// </summary>
        /// <param name="e">Event arguments for the event.</param>
        private static void OnClipboardUpdate(EventArgs e)
        {
            ClipboardUpdate(ClipboardForm, EventArgs.Empty);
        }

        /// <summary>
        /// Hidden form to recieve the WM_CLIPBOARDUPDATE message.
        /// </summary>
        private class NotificationForm : Form
        {
            public NotificationForm()
            {
                NativeMethods.SetParent(Handle, NativeMethods.HWND_MESSAGE);
                NativeMethods.AddClipboardFormatListener(Handle);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
                {
                    OnClipboardUpdate(null);
                }
                base.WndProc(ref m);
            }
        }

        private static class NativeMethods
        {
            // See http://msdn.microsoft.com/en-us/library/ms649021%28v=vs.85%29.aspx
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
        }
    }
}