using System;
using System.ComponentModel;
using System.Drawing;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PoeShared.UI;

namespace PoeShared.Blazor.Wpf;

internal sealed class BlazorWindowViewController : DisposableReactiveObject, IBlazorWindowViewController
{
    private readonly IBlazorWindow blazorWindow;

    public BlazorWindowViewController([NotNull] IBlazorWindow blazorWindow)
    {
        this.blazorWindow = blazorWindow ?? throw new ArgumentNullException(nameof(blazorWindow));
    }

    public void Hide()
    {
        blazorWindow.Hide();
    }

    public void Show()
    {
        blazorWindow.Show();
    }

    public IObservable<Unit> WhenLoaded => blazorWindow.WhenLoaded.Select(x => Unit.Default);

    public IObservable<Unit> WhenUnloaded  => blazorWindow.WhenUnloaded.Select(x => Unit.Default);

    public IObservable<Unit> WhenDeactivated => blazorWindow.WhenDeactivated.Select(x => Unit.Default);

    public IObservable<Unit> WhenActivated => blazorWindow.WhenActivated.Select(x => Unit.Default);

    public IObservable<Unit> WhenClosed => blazorWindow.WhenClosed.Select(x => Unit.Default);

    public IObservable<CancelEventArgs> WhenClosing => blazorWindow.WhenClosing;

    public IObservable<Unit> WhenRendered => WhenLoaded; //FIXME Detect first full render in Blazor?

    public IObservable<KeyEventArgs> WhenKeyUp => blazorWindow.WhenKeyUp;

    public IObservable<KeyEventArgs> WhenKeyDown => blazorWindow.WhenKeyDown;

    public IObservable<KeyEventArgs> WhenPreviewKeyDown => blazorWindow.WhenPreviewKeyDown;

    public IObservable<KeyEventArgs> WhenPreviewKeyUp => blazorWindow.WhenPreviewKeyUp;

    public IntPtr Handle => blazorWindow.GetWindowHandle();

    public Rectangle NativeBounds
    {
        get => blazorWindow.GetWindowRect();
        set => blazorWindow.SetWindowRect(value);
    }

    public Window Window
    {
        get
        {
            if (blazorWindow is not IBlazorWindowMetroController metroController)
            {
                throw new NotSupportedException($"Underlying window does not implement {typeof(IBlazorWindowMetroController)}");
            }

            return metroController.GetWindow();
        }
    }

    public IBlazorWindow BlazorWindow => blazorWindow;
    
    public void EnsureCreated()
    {
        if (blazorWindow is not IBlazorWindowMetroController metroController)
        {
            throw new NotSupportedException($"Underlying window does not implement {typeof(IBlazorWindowMetroController)}");
        }

        metroController.EnsureCreated();
    }

    public bool Topmost
    {
        get => blazorWindow.Topmost;
        set => blazorWindow.Topmost = value;
    }

    public void TakeScreenshot(string fileName)
    {
        throw new NotSupportedException();
    }

    public void Minimize()
    {
        blazorWindow.Minimize();
    }

    public void Activate()
    {
        throw new NotImplementedException();
    }

    public void Close(bool? result)
    {
        blazorWindow.Close();
    }

    public void Close()
    {
        blazorWindow.Close();
    }
}