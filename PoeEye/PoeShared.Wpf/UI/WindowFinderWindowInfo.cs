using System;
using PoeShared.Native;

namespace PoeShared.UI;

internal sealed class WindowFinderWindowInfo
{
    public WindowFinderWindowInfo(IWindowHandle windowHandle)
    {
        WindowHandle = windowHandle;
    }

    public IWindowHandle WindowHandle { get; }

    public string Title => $"{WindowHandle.Title??"<no title>"}";
    
    public WinRect Bounds => WindowHandle.DwmFrameBounds;
    
    public override string ToString()
    {
        return Title;
    }
}