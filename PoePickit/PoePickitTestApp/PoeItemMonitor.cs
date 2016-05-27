using System;
using System.Reactive.Linq;
using System.Windows.Forms;
using ClipboardMonitor;

namespace PoePickitTestApp
{
    public class PoeItemMonitor
    {
        private readonly IDisposable clipboardObservable;

        public PoeItemMonitor()
        {
            var textFromClipboard = Observable
                .FromEventPattern<EventHandler, EventArgs>(
                    h => ClipboardNotifications.ClipboardUpdate += h,
                    h => ClipboardNotifications.ClipboardUpdate -= h)
                .Select(x => GetTextFromClipboard())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Publish();

            PoeItemsSource = textFromClipboard;

            clipboardObservable = textFromClipboard.Connect();
        }

        public IObservable<string> PoeItemsSource { get; }

        private string GetTextFromClipboard()
        {
            try
            {
                if (!Clipboard.ContainsText())
                {
                    return null;
                }

                var textFromClipboard = Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(textFromClipboard))
                {
                    return null;
                }

                return textFromClipboard;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}