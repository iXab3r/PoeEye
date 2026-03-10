namespace PoeShared.Blazor.Wpf;

internal sealed class BlazorWindowAccessor : IBlazorWindowAccessor
{
    public IBlazorWindow Window { get; }

    public BlazorWindowAccessor(IBlazorWindow window)
    {
        Window = window;
    }
}