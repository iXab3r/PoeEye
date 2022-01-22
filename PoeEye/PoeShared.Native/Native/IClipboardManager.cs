using System;
using System.Windows.Forms;
using JetBrains.Annotations;

namespace PoeShared.Native;

public interface IClipboardManager
{
    TimeSpan ClipboardRestorationTimeout { get; }

    int ClipboardSetRetryCount { get; }

    void SetText([NotNull] string text);

    IDataObject GetDataObject();

    void SetDataObject(object dataObject);

    string GetText();
}