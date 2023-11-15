using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Annotations;

namespace PoeShared.Native;

public interface IClipboardManager
{
    TimeSpan ClipboardRestorationTimeout { get; }

    int ClipboardSetRetryCount { get; }
    
    bool Clear();

    void SetText([NotNull] string text);
    
    void SetImage(Image image);

    void SetDataObject(object dataObject);

    string GetText();
    
    Image GetImage();
    
    IReadOnlyList<string> GetFileDropList();

    IDataObject GetDataObject();
    
    bool ContainsImage();
    
    bool ContainsText();

    bool ContainsFileDropList();
}