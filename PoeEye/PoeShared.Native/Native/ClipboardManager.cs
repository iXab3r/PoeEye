using System;
using System.Windows.Forms;

using log4net;
using PoeShared.Logging;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Native
{
    internal sealed class ClipboardManager : IClipboardManager
    {
        private static readonly IFluentLog Log = typeof(ClipboardManager).PrepareLogger();

        public TimeSpan ClipboardRestorationTimeout { get; } = TimeSpan.FromMilliseconds(200);

        public int ClipboardSetRetryCount { get; } = 10;

        public void SetText(string text)
        {
            Guard.ArgumentNotNull(() => text);

            SetDataObject(text);
        }

        public IDataObject GetDataObject()
        {
            return Clipboard.GetDataObject();
        }

        public void SetDataObject(object dataObject)
        {
            Log.Debug(
                $"[PoeChatService] Setting new clipboard object '{dataObject}' (retry: {ClipboardSetRetryCount}, timeout: {ClipboardRestorationTimeout})...");
            Clipboard.SetDataObject(dataObject, true, ClipboardSetRetryCount, (int) ClipboardRestorationTimeout.TotalMilliseconds);
        }

        public string GetText()
        {
            return Clipboard.GetText();
        }
    }
}