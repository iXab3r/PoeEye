using System;
using System.Collections.Generic;
using System.Windows.Forms;
using JetBrains.Annotations;

namespace PoeShared.Native;

public interface IClipboardManager
{
    TimeSpan ClipboardRestorationTimeout { get; }

    int ClipboardSetRetryCount { get; }

    void SetText([NotNull] string text);

    bool ContainsText();

    bool ContainsFileDropList();

    IReadOnlyList<string> GetFileDropList();

    IDataObject GetDataObject();

    void SetDataObject(object dataObject);

    string GetText();

    bool Clear();
}