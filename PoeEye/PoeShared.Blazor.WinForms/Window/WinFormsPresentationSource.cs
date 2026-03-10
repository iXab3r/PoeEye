using System;
using System.Windows;
using System.Windows.Media;

namespace PoeShared.Blazor.WinForms;

internal sealed class WinFormsPresentationSource : PresentationSource
{
    public static WinFormsPresentationSource Instance { get; } = new();

    public override Visual RootVisual
    {
        get => null;
        set { }
    }

    public override bool IsDisposed => false;

    protected override CompositionTarget GetCompositionTargetCore()
    {
        return null;
    }
}
