using System.Windows;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public interface IOverlayViewModel : IDisposableReactiveObject
    {
        Point Location { get; }

        Size Size { get; }
    }
}