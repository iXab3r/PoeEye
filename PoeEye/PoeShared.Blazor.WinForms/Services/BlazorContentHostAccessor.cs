namespace PoeShared.Blazor.WinForms.Services;

internal sealed class BlazorContentHostAccessor : IBlazorContentHostAccessor
{
    public BlazorContentHostAccessor(BlazorContentHost contentControl)
    {
        Control = contentControl;
    }

    public BlazorContentHost Control { get; }
}
