using System.Drawing;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

public interface IWindowBoundsTracker : IDisposableReactiveObject
{
    IWindowHandle Window { get; set; }
    
    Rectangle? Bounds { get; }
}