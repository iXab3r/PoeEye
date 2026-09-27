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
    
    /// <summary>
    /// Publishes PNG with transparency and a legacy Bitmap representation on an STA thread.
    /// The caller retains ownership of <paramref name="image"/> and may dispose it after this call.
    /// </summary>
    void SetImage(Image image);

    void SetDataObject(object dataObject);

    string GetText();
    
    /// <summary>
    /// Reads PNG preferentially, falling back to Bitmap when PNG is unavailable or rejected by the decoder.
    /// The caller owns the returned image and must dispose it; returns null when no image is available.
    /// </summary>
    Image GetImage();
    
    IReadOnlyList<string> GetFileDropList();

    IDataObject GetDataObject();
    
    /// <summary>
    /// Checks for PNG or Bitmap availability without decoding the image payload.
    /// </summary>
    bool ContainsImage();
    
    bool ContainsText();

    bool ContainsFileDropList();
}
