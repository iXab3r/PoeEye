using PoeShared.Native;

namespace PoeShared.UI;

public readonly record struct WindowFinderMatch
{
    public required IWindowHandle Window { get; init; }
    
    public WinPoint CursorLocation { get; init; }
}