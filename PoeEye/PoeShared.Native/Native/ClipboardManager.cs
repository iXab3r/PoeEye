using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Forms;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

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

    public bool ContainsText()
    {
        return Clipboard.ContainsText();
    }
    
    public bool ContainsFileDropList()
    {
        return Clipboard.ContainsFileDropList();
    }

    public IReadOnlyList<string> GetFileDropList()
    {
        var files = Clipboard.GetFileDropList();
        var result = new List<string>();
        foreach (var file in files)
        {
            file.AddTo(result);
        }
        return result;
    }

    public string GetText()
    {
        return Clipboard.GetText();
    }
}