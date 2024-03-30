namespace PoeShared.Blazor.Wpf.Services;

internal sealed class BlazorContentControlAccessor : IBlazorContentControlAccessor
{
    public BlazorContentControlAccessor(BlazorContentControl contentControl)
    {
        this.Control = contentControl;
    }

    public BlazorContentControl Control { get; }
}