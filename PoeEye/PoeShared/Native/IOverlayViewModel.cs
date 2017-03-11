using System.Windows;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public interface IOverlayViewModel : IDisposableReactiveObject
    {
        double Left { get; }

        double Top { get; }

        double Width { get; }

        double Height { get; }

        Size MinSize { get; }

        Size MaxSize { get; }

        bool IsLocked { get; }

        object Header { get; }
    }
}